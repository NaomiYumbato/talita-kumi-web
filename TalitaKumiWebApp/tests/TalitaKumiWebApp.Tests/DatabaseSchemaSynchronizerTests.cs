using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TalitaKumiWebApp.Data;
using TalitaKumiWebApp.Models;
using Xunit;

namespace TalitaKumiWebApp.Tests;

public class DatabaseSchemaSynchronizerTests
{
    [Fact]
    public async Task SynchronizeAsync_EnBaseEnMemoria_EjecutaSinErrores()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var synchronizer = new DatabaseSchemaSynchronizer(NullLogger<DatabaseSchemaSynchronizer>.Instance);

        var result = await synchronizer.SynchronizeAsync(context);

        Assert.NotNull(result);
        Assert.True(result.DatabaseCreated);
    }

    [Fact]
    public void BuildAddColumnSql_ParaPropiedadDecimal_GeneraAlterTableConDefault()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=Dummy;TrustServerCertificate=True;")
            .Options;

        using var context = new AppDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Reserva));
        Assert.NotNull(entityType);

        var propMonto = entityType.FindProperty(nameof(Reserva.MontoAbonado));
        Assert.NotNull(propMonto);

        var sql = DatabaseSchemaSynchronizer.BuildAddColumnSql("Reservas", propMonto);

        Assert.Contains("ALTER TABLE [Reservas] ADD [MontoAbonado]", sql);
        Assert.Contains("decimal(10,2)", sql);
        Assert.Contains("NOT NULL DEFAULT", sql);
    }

    [Fact]
    public void BuildAddColumnSql_ParaPropiedadNullable_GeneraAlterTableNull()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=Dummy;TrustServerCertificate=True;")
            .Options;

        using var context = new AppDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Reserva));
        Assert.NotNull(entityType);

        var propNotas = entityType.FindProperty(nameof(Reserva.Notas));
        Assert.NotNull(propNotas);

        var sql = DatabaseSchemaSynchronizer.BuildAddColumnSql("Reservas", propNotas);

        Assert.Contains("ALTER TABLE [Reservas] ADD [Notas]", sql);
        Assert.Contains("nvarchar(500)", sql);
        Assert.Contains("NULL;", sql);
    }

    [Fact]
    public async Task SynchronizeAsync_EjecucionIdempotente_NoFallaEnMultiplesLlamadas()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var synchronizer = new DatabaseSchemaSynchronizer(NullLogger<DatabaseSchemaSynchronizer>.Instance);

        var result1 = await synchronizer.SynchronizeAsync(context);
        var result2 = await synchronizer.SynchronizeAsync(context);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }
}
