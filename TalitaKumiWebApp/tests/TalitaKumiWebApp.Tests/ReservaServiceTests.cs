using Microsoft.EntityFrameworkCore;
using TalitaKumiWebApp.Data;
using TalitaKumiWebApp.Models;
using TalitaKumiWebApp.Services;
using Xunit;

namespace TalitaKumiWebApp.Tests;

public class ReservaServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ReservaService _service;

    public ReservaServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _service = new ReservaService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CrearReserva_DatosValidos_GuardaEnBaseDeDatosYAsignaId()
    {
        // Arrange
        var reserva = new Reserva
        {
            Nombre = "Maria Sanchez",
            Telefono = "+51 987 654 321",
            Servicio = "Soft Gel Tips",
            PrecioEstimado = 120m,
            DuracionMinutos = 125,
            Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Horario = "Tarde (14:00 - 16:30)",
            Notas = "Con retiro de semi"
        };

        // Act
        var guardada = await _service.CrearReservaAsync(reserva);

        // Assert
        Assert.True(guardada.Id > 0);
        Assert.Equal("Pendiente", guardada.Estado);

        var enDb = await _service.ObtenerPorIdAsync(guardada.Id);
        Assert.NotNull(enDb);
        Assert.Equal("Maria Sanchez", enDb.Nombre);
        Assert.Equal(120m, enDb.PrecioEstimado);
    }

    [Fact]
    public async Task CrearReserva_NombreVacio_LanzaArgumentException()
    {
        // Arrange
        var reserva = new Reserva
        {
            Nombre = "",
            Telefono = "987654321",
            Servicio = "Manicura",
            Fecha = DateOnly.FromDateTime(DateTime.Today)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CrearReservaAsync(reserva));
    }

    [Fact]
    public async Task CrearReserva_FechaAnterior_LanzaArgumentException()
    {
        // Arrange
        var reserva = new Reserva
        {
            Nombre = "Lucia",
            Telefono = "987654321",
            Servicio = "Lifting",
            Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(-1))
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CrearReservaAsync(reserva));
    }
}
