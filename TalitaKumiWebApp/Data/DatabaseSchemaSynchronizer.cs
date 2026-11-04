using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace TalitaKumiWebApp.Data;

public class DatabaseSchemaSynchronizer : IDatabaseSchemaSynchronizer
{
    private readonly ILogger<DatabaseSchemaSynchronizer> _logger;

    public DatabaseSchemaSynchronizer(ILogger<DatabaseSchemaSynchronizer> logger)
    {
        _logger = logger;
    }

    public async Task<SchemaSyncResult> SynchronizeAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        var result = new SchemaSyncResult();

        // 1. Compatibilidad para pruebas unitarias con proveedores en memoria
        if (!context.Database.IsRelational())
        {
            result.DatabaseCreated = await context.Database.EnsureCreatedAsync(cancellationToken);
            return result;
        }

        // 2. Si la base de datos no existe en el servidor, EnsureCreated crea todo el esquema inicial
        var creada = await context.Database.EnsureCreatedAsync(cancellationToken);
        if (creada)
        {
            result.DatabaseCreated = true;
            _logger.LogInformation("[SchemaSync] Base de datos creada desde cero con todas sus tablas e índices.");
            return result;
        }

        // 3. La base de datos ya existía: detectar tablas, columnas e índices que falten
        var tablasExistentes = await GetExistingTableNamesAsync(context, cancellationToken);
        var indicesExistentes = await GetExistingIndexNamesAsync(context, cancellationToken);

        // A. Verificar y crear tablas faltantes e índices a partir del script DDL generado por EF Core
        var scriptDdl = context.Database.GenerateCreateScript();
        var batches = SplitDdlScript(scriptDdl);

        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // Detección de CREATE TABLE
            var matchTable = Regex.Match(trimmed, @"CREATE\s+TABLE\s+(?:\[?(?:\w+)\]?\.)?\[?(\w+)\]?", RegexOptions.IgnoreCase);
            if (matchTable.Success)
            {
                var tableName = matchTable.Groups[1].Value;
                if (!tablasExistentes.Contains(tableName))
                {
                    _logger.LogInformation("[SchemaSync] Detectada tabla faltante: '{Table}'. Creando...", tableName);
                    await context.Database.ExecuteSqlRawAsync(trimmed, cancellationToken);
                    tablasExistentes.Add(tableName);
                    result.TablesCreated.Add(tableName);
                }
                continue;
            }

            // Detección de CREATE INDEX
            var matchIndex = Regex.Match(trimmed, @"CREATE\s+(?:UNIQUE\s+)?INDEX\s+\[?(\w+)\]?", RegexOptions.IgnoreCase);
            if (matchIndex.Success)
            {
                var indexName = matchIndex.Groups[1].Value;
                if (!indicesExistentes.Contains(indexName))
                {
                    try
                    {
                        _logger.LogInformation("[SchemaSync] Detectado índice faltante: '{Index}'. Creando...", indexName);
                        await context.Database.ExecuteSqlRawAsync(trimmed, cancellationToken);
                        indicesExistentes.Add(indexName);
                        result.IndexesCreated.Add(indexName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[SchemaSync] No se pudo crear el índice '{Index}'. Puede que dependa de otra estructura.", indexName);
                    }
                }
            }
        }

        // B. Verificar y agregar columnas faltantes en tablas existentes
        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrEmpty(tableName) || !tablasExistentes.Contains(tableName)) continue;

            var columnasExistentes = await GetExistingColumnNamesAsync(context, tableName, cancellationToken);

            foreach (var property in entityType.GetProperties())
            {
                var colName = property.GetColumnName();
                if (string.IsNullOrEmpty(colName) || columnasExistentes.Contains(colName)) continue;

                var alterSql = BuildAddColumnSql(tableName, property);
                _logger.LogInformation("[SchemaSync] Detectada columna faltante '{Table}.{Column}'. Ejecutando: {Sql}", tableName, colName, alterSql);

                await context.Database.ExecuteSqlRawAsync(alterSql, cancellationToken);
                columnasExistentes.Add(colName);
                result.ColumnsAdded.Add($"{tableName}.{colName}");
            }
        }

        // C. Normalizar formatos históricos de turnos en Reservas
        if (tablasExistentes.Contains("Reservas"))
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    "UPDATE [Reservas] SET [Horario] = 'Tarde 1 (13:00 a 16:30)' WHERE [Horario] = 'Tarde Temprano (13:00 a 16:30)'; " +
                    "UPDATE [Reservas] SET [Horario] = 'Tarde 2 (16:30 a 19:30)' WHERE [Horario] = 'Tarde Tarde (16:30 a 19:30)';",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SchemaSync] Advertencia al normalizar horarios en Reservas.");
            }
        }

        if (result.HasChanges)
        {
            _logger.LogInformation("[SchemaSync] Sincronización completada: {Tablas} tablas, {Cols} columnas, {Indices} índices agregados.",
                result.TablesCreated.Count, result.ColumnsAdded.Count, result.IndexesCreated.Count);
        }

        return result;
    }

    public static string BuildAddColumnSql(string tableName, IProperty property)
    {
        var columnName = property.GetColumnName();
        var columnType = property.GetColumnType();

        if (property.IsNullable)
        {
            return $"ALTER TABLE [{tableName}] ADD [{columnName}] {columnType} NULL;";
        }

        var defaultValSql = property.GetDefaultValueSql();
        if (string.IsNullOrWhiteSpace(defaultValSql))
        {
            var defaultVal = property.GetDefaultValue();
            if (defaultVal != null)
            {
                defaultValSql = defaultVal is bool b ? (b ? "1" : "0")
                    : defaultVal is string s ? $"'{s.Replace("'", "''")}'"
                    : defaultVal.ToString();
            }
            else
            {
                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (clrType == typeof(bool)) defaultValSql = "0";
                else if (clrType == typeof(int) || clrType == typeof(long) || clrType == typeof(short) || clrType == typeof(byte)) defaultValSql = "0";
                else if (clrType == typeof(decimal) || clrType == typeof(double) || clrType == typeof(float)) defaultValSql = "0.00";
                else if (clrType == typeof(DateTime)) defaultValSql = "GETUTCDATE()";
                else if (clrType == typeof(DateOnly)) defaultValSql = "CAST(GETDATE() AS date)";
                else if (clrType == typeof(Guid)) defaultValSql = "NEWID()";
                else if (clrType == typeof(string)) defaultValSql = "''";
                else defaultValSql = "0";
            }
        }

        return $"ALTER TABLE [{tableName}] ADD [{columnName}] {columnType} NOT NULL DEFAULT {defaultValSql};";
    }

    private static List<string> SplitDdlScript(string script)
    {
        if (string.IsNullOrWhiteSpace(script)) return new List<string>();

        return Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static async Task<HashSet<string>> GetExistingTableNamesAsync(AppDbContext context, CancellationToken ct)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = context.Database.GetDbConnection();
        var shouldClose = false;

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
            shouldClose = true;
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                tables.Add(reader.GetString(0));
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return tables;
    }

    private static async Task<HashSet<string>> GetExistingColumnNamesAsync(AppDbContext context, string tableName, CancellationToken ct)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = context.Database.GetDbConnection();
        var shouldClose = false;

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
            shouldClose = true;
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName";
            var param = cmd.CreateParameter();
            param.ParameterName = "@tableName";
            param.Value = tableName;
            cmd.Parameters.Add(param);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                columns.Add(reader.GetString(0));
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return columns;
    }

    private static async Task<HashSet<string>> GetExistingIndexNamesAsync(AppDbContext context, CancellationToken ct)
    {
        var indexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = context.Database.GetDbConnection();
        var shouldClose = false;

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
            shouldClose = true;
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT name FROM sys.indexes WHERE name IS NOT NULL";
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                indexes.Add(reader.GetString(0));
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return indexes;
    }
}
