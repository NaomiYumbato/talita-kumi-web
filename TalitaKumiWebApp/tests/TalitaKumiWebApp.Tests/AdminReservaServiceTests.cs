using Microsoft.EntityFrameworkCore;
using TalitaKumiWebApp.Data;
using TalitaKumiWebApp.Models;
using TalitaKumiWebApp.Services;
using TalitaKumiWebApp.ViewModels.Admin;
using Xunit;

namespace TalitaKumiWebApp.Tests;

public class AdminReservaServiceTests
{
    [Fact]
    public async Task ActualizarEstado_ACampoConfirmada_ActualizaEstadoYMontoAbonado()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new ReservaService(context);

        var r = new Reserva
        {
            Nombre = "Ana Silva",
            Telefono = "999112233",
            Servicio = "Corte Bob",
            Fecha = DateOnly.FromDateTime(DateTime.Today),
            Horario = "Mañana (10:00 a 13:00)",
            Estado = "Pendiente",
            MontoAbonado = 0m,
            PrecioEstimado = 65m
        };
        context.Reservas.Add(r);
        await context.SaveChangesAsync();

        var ok = await service.ActualizarEstadoAsync(r.Id, "Reserva Confirmada");
        Assert.True(ok);

        var actualizada = await context.Reservas.FindAsync(r.Id);
        Assert.NotNull(actualizada);
        Assert.Equal("Confirmada", actualizada.Estado);
        Assert.Equal(20.00m, actualizada.MontoAbonado);
    }

    [Fact]
    public async Task ActualizarNotas_GuardaTextoCorrectamente()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new ReservaService(context);

        var r = new Reserva
        {
            Nombre = "Beatriz Luna",
            Telefono = "988223344",
            Servicio = "Balayage",
            Fecha = DateOnly.FromDateTime(DateTime.Today),
            Horario = "Tarde 1 (13:00 a 16:30)",
            Notas = "Nota inicial"
        };
        context.Reservas.Add(r);
        await context.SaveChangesAsync();

        var ok = await service.ActualizarNotasAsync(r.Id, "Tinte 7.1 + agua 20 vol");
        Assert.True(ok);

        var actualizada = await context.Reservas.FindAsync(r.Id);
        Assert.NotNull(actualizada);
        Assert.Equal("Tinte 7.1 + agua 20 vol", actualizada.Notas);
    }

    [Fact]
    public async Task ObtenerReservasSemana_YFiltradas_FiltraCorrectamente()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new ReservaService(context);

        var lunes = new DateOnly(2026, 10, 19);
        var miercoles = new DateOnly(2026, 10, 21);
        var domingoFuera = new DateOnly(2026, 10, 26);

        context.Reservas.AddRange(
            new Reserva { Nombre = "Carla", Telefono = "911", Servicio = "Kapping", Fecha = lunes, Horario = "Mañana", Estado = "Pendiente" },
            new Reserva { Nombre = "Daniela", Telefono = "922", Servicio = "Lifting", Fecha = miercoles, Horario = "Tarde 1", Estado = "Confirmada" },
            new Reserva { Nombre = "Elena", Telefono = "933", Servicio = "Botox", Fecha = domingoFuera, Horario = "Mañana", Estado = "Pendiente" }
        );
        await context.SaveChangesAsync();

        var semana = await service.ObtenerReservasSemanaAsync(lunes, new DateOnly(2026, 10, 25));
        Assert.Equal(2, semana.Count);

        var filtradasPorEstado = await service.ObtenerReservasFiltradasAsync(fecha: null, busqueda: null, estado: "Confirmada");
        Assert.Single(filtradasPorEstado);
        Assert.Equal("Daniela", filtradasPorEstado[0].Nombre);

        var filtradasPorBusqueda = await service.ObtenerReservasFiltradasAsync(fecha: null, busqueda: "Carla", estado: null);
        Assert.Single(filtradasPorBusqueda);
    }

    [Fact]
    public async Task CrearReservaManual_CreaReservaYAsociaCliente()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var s = new Servicio { Nombre = "Soft Gel Tips", Categoria = "Uñas", PrecioBase = 90m, DuracionBaseMinutos = 90, Activo = true };
        context.Servicios.Add(s);
        await context.SaveChangesAsync();

        var service = new ReservaService(context);
        var model = new CrearReservaManualViewModel
        {
            Nombre = "Fernanda Rios",
            Telefono = "977112233",
            ServicioId = s.Id,
            Fecha = new DateOnly(2026, 10, 24),
            Horario = "Tarde 1 (13:00 a 16:30)",
            Precio = 90m,
            MontoAbonado = 20m,
            Notas = "Abonó en efectivo en salón",
            EstadoInicial = "Confirmada"
        };

        var creada = await service.CrearReservaManualAsync(model);
        Assert.NotNull(creada);
        Assert.True(creada.Id > 0);
        Assert.Equal("Confirmada", creada.Estado);
        Assert.Equal(20m, creada.MontoAbonado);
        Assert.Equal(90m, creada.PrecioEstimado);
        Assert.Equal(s.Id, creada.ServicioId);

        var cliente = await context.Clientes.FirstOrDefaultAsync(c => c.Telefono == "977112233");
        Assert.NotNull(cliente);
        Assert.Equal("Fernanda Rios", cliente.Nombre);
    }
}
