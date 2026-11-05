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

    [Fact]
    public async Task CrearReserva_DesdeCotizacionBot_AsignaServicioIdCorrectamente()
    {
        // Arrange - Reserva con texto generado por el bot
        var reserva = new Reserva
        {
            Nombre = "Valeria Ramos",
            Telefono = "+51 912 345 678",
            Servicio = "Soft Gel (Largo Medio, Arte Francesita)",
            PrecioEstimado = 120m,
            DuracionMinutos = 125,
            Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Horario = "Tarde (14:00 - 16:30)"
        };

        // Act
        var guardada = await _service.CrearReservaAsync(reserva);

        // Assert
        Assert.NotNull(guardada.ServicioId);
        Assert.Equal(3, guardada.ServicioId); // Soft Gel Tips tiene Id = 3
    }

    [Fact]
    public async Task CrearReserva_ConServicioIdExplicito_AsignaServicioIdDirecto()
    {
        // Arrange
        var reserva = new Reserva
        {
            Nombre = "Camila Gomez",
            Telefono = "+51 999 888 777",
            ServicioId = 2, // Kapping Gel
            Servicio = "Kapping Gel",
            PrecioEstimado = 85m,
            DuracionMinutos = 90,
            Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Horario = "Mañana"
        };

        // Act
        var guardada = await _service.CrearReservaAsync(reserva);

        // Assert
        Assert.Equal(2, guardada.ServicioId);
    }

    [Fact]
    public async Task CrearReserva_TurnoYaOcupado_LanzaInvalidOperationException()
    {
        // Arrange
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(5));
        var primera = new Reserva
        {
            Nombre = "Primera Clienta",
            Telefono = "911111111",
            Servicio = "Kapping Gel",
            Fecha = fecha,
            Horario = "Mañana (10:00 a 13:00)",
            Estado = "Pendiente"
        };
        await _service.CrearReservaAsync(primera);

        var segundaMismoTurno = new Reserva
        {
            Nombre = "Segunda Clienta",
            Telefono = "922222222",
            Servicio = "Soft Gel Tips",
            Fecha = fecha,
            Horario = "Mañana (10:00 a 13:00)",
            Estado = "Pendiente"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CrearReservaAsync(segundaMismoTurno));
        Assert.Contains("ocupado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrearReserva_TurnoOcupadoPeroCancelado_PermiteCrearReserva()
    {
        // Arrange
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(6));
        var cancelada = new Reserva
        {
            Nombre = "Clienta Cancelada",
            Telefono = "933333333",
            Servicio = "Kapping Gel",
            Fecha = fecha,
            Horario = "Tarde 1 (13:00 a 16:30)",
            Estado = "Cancelada"
        };
        _context.Reservas.Add(cancelada);
        await _context.SaveChangesAsync();

        var nuevaReserva = new Reserva
        {
            Nombre = "Nueva Clienta",
            Telefono = "944444444",
            Servicio = "Soft Gel Tips",
            Fecha = fecha,
            Horario = "Tarde 1 (13:00 a 16:30)",
            Estado = "Pendiente"
        };

        // Act
        var guardada = await _service.CrearReservaAsync(nuevaReserva);

        // Assert
        Assert.NotNull(guardada);
        Assert.True(guardada.Id > 0);
    }

    [Fact]
    public async Task ObtenerDisponibilidadFechaAsync_RetornaTurnosLibresYOcupados()
    {
        // Arrange
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var r1 = new Reserva
        {
            Nombre = "Clienta Mañana",
            Telefono = "955555555",
            Servicio = "Kapping Gel",
            Fecha = fecha,
            Horario = "Mañana (10:00 a 13:00)",
            Estado = "Confirmada"
        };
        await _service.CrearReservaAsync(r1);

        // Act
        var disp = await _service.ObtenerDisponibilidadFechaAsync(fecha);

        // Assert
        Assert.Equal(fecha, disp.Fecha);
        Assert.Contains("Mañana", disp.TurnosOcupados[0]);
        Assert.False(disp.EstaCompletamenteOcupado);
        Assert.Equal(2, disp.TurnosDisponibles.Count);
        Assert.DoesNotContain(disp.TurnosDisponibles, t => t.Contains("Mañana"));
    }

    [Fact]
    public async Task ObtenerFechasCompletamenteOcupadasAsync_RetornaSoloFechasCon3TurnosOcupados()
    {
        // Arrange
        var fechaCompleta = DateOnly.FromDateTime(DateTime.Today.AddDays(8));
        var fechaIncompleta = DateOnly.FromDateTime(DateTime.Today.AddDays(9));

        _context.Reservas.AddRange(
            new Reserva { Nombre = "A", Telefono = "1", Servicio = "S", Fecha = fechaCompleta, Horario = "Mañana", Estado = "Confirmada" },
            new Reserva { Nombre = "B", Telefono = "2", Servicio = "S", Fecha = fechaCompleta, Horario = "Tarde 1", Estado = "Pendiente" },
            new Reserva { Nombre = "C", Telefono = "3", Servicio = "S", Fecha = fechaCompleta, Horario = "Tarde 2", Estado = "Completada" },
            new Reserva { Nombre = "D", Telefono = "4", Servicio = "S", Fecha = fechaIncompleta, Horario = "Mañana", Estado = "Confirmada" }
        );
        await _context.SaveChangesAsync();

        // Act
        var fechas = await _service.ObtenerFechasCompletamenteOcupadasAsync(
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(15))
        );

        // Assert
        Assert.Contains(fechaCompleta, fechas);
        Assert.DoesNotContain(fechaIncompleta, fechas);
    }
}
