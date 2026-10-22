using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using TalitaKumiWebApp.Controllers;
using TalitaKumiWebApp.Data;
using TalitaKumiWebApp.Models;
using TalitaKumiWebApp.Services;
using TalitaKumiWebApp.ViewModels.Admin;
using Xunit;

namespace TalitaKumiWebApp.Tests;

public class AdminDashboardControllerTests
{
    [Fact]
    public async Task Index_GET_RetornaDashboardViewModelConReservasYSemanas()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var authService = new AdminAuthService(context);
        var reservaService = new ReservaService(context);

        var controller = new AdminController(authService, reservaService);

        var result = await controller.Index(fecha: null, busqueda: null, estado: null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AdminDashboardViewModel>(viewResult.Model);
        Assert.True(model.InicioSemana <= model.FechaSeleccionada);
        Assert.True(model.FinSemana >= model.FechaSeleccionada);
    }

    [Fact]
    public async Task ActualizarEstado_POST_ActualizaEstadoYDevuelveJson()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var authService = new AdminAuthService(context);
        var reservaService = new ReservaService(context);

        var r = new Reserva { Nombre = "Paola", Telefono = "955", Servicio = "Pedicura", Fecha = DateOnly.FromDateTime(DateTime.Today), Horario = "Mañana", Estado = "Pendiente" };
        context.Reservas.Add(r);
        await context.SaveChangesAsync();

        var controller = new AdminController(authService, reservaService);
        var req = new ActualizarEstadoRequest { Id = r.Id, Estado = "🟢 Reserva Confirmada" };

        var result = await controller.ActualizarEstado(req);

        var json = Assert.IsType<JsonResult>(result);
        Assert.NotNull(json.Value);

        var enDb = await context.Reservas.FindAsync(r.Id);
        Assert.Equal("Confirmada", enDb!.Estado);
        Assert.Equal(20.00m, enDb.MontoAbonado);
    }

    [Fact]
    public async Task ActualizarNotas_POST_ActualizaNotasYDevuelveJson()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var authService = new AdminAuthService(context);
        var reservaService = new ReservaService(context);

        var r = new Reserva { Nombre = "Rosa", Telefono = "944", Servicio = "Uñas", Fecha = DateOnly.FromDateTime(DateTime.Today), Horario = "Tarde 1", Notas = null };
        context.Reservas.Add(r);
        await context.SaveChangesAsync();

        var controller = new AdminController(authService, reservaService);
        var req = new ActualizarNotasRequest { Id = r.Id, Notas = "Alergia a gel semipermanente" };

        var result = await controller.ActualizarNotas(req);

        var json = Assert.IsType<JsonResult>(result);
        Assert.NotNull(json.Value);

        var enDb = await context.Reservas.FindAsync(r.Id);
        Assert.Equal("Alergia a gel semipermanente", enDb!.Notas);
    }

    [Fact]
    public async Task CrearReservaManual_POST_CreaReservaYRedirige()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var s = new Servicio { Nombre = "Corte", Categoria = "Cabello", PrecioBase = 50m, DuracionBaseMinutos = 45, Activo = true };
        context.Servicios.Add(s);
        await context.SaveChangesAsync();

        var authService = new AdminAuthService(context);
        var reservaService = new ReservaService(context);

        var controller = new AdminController(authService, reservaService)
        {
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), new MockTempDataProvider())
        };

        var model = new CrearReservaManualViewModel
        {
            Nombre = "Gloria Paz",
            Telefono = "911223344",
            ServicioId = s.Id,
            Fecha = new DateOnly(2026, 10, 24),
            Horario = "Mañana (10:00 a 13:00)",
            Precio = 50m
        };

        var result = await controller.CrearReservaManual(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Index), redirect.ActionName);

        var creada = await context.Reservas.FirstOrDefaultAsync(r => r.Telefono == "911223344");
        Assert.NotNull(creada);
        Assert.Equal("Gloria Paz", creada.Nombre);
    }

    private class MockTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();
        public IDictionary<string, object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context) => _data;
        public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context, IDictionary<string, object> values) { }
    }
}
