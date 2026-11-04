namespace TalitaKumiWebApp.Data;

public interface IDatabaseSchemaSynchronizer
{
    /// <summary>
    /// Compara el modelo de EF Core (AppDbContext) con la base de datos real (SQL Server)
    /// y crea de forma dinámica e idempotente cualquier tabla, columna o índice faltante.
    /// </summary>
    Task<SchemaSyncResult> SynchronizeAsync(AppDbContext context, CancellationToken cancellationToken = default);
}

public class SchemaSyncResult
{
    public bool DatabaseCreated { get; set; }
    public List<string> TablesCreated { get; } = new();
    public List<string> ColumnsAdded { get; } = new();
    public List<string> IndexesCreated { get; } = new();

    public bool HasChanges => DatabaseCreated || TablesCreated.Count > 0 || ColumnsAdded.Count > 0 || IndexesCreated.Count > 0;
}
