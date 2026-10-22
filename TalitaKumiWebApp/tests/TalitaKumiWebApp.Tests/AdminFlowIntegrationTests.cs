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

public class AdminFlowIntegrationTests
{
    [Fact]
    public async Task FlujoCompleto_CrearCitaManual_CambiarEstadoAConfirmada_YActualizarNotas()
    {
        // 1. Arrange Database and Services
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var authService = new AdminAuthService(context);
        var reservaService = new ReservaService(context);

        // Prepopulate catalog service
        var servicio = new Servicio
        {
            Nombre = "Balayage Caramelo",
            Categoria = "Color",
            PrecioBase = 180m,
            DuracionBaseMinutos = 180,
            Activo = true
        };
        context.Servicios.Add(servicio);
        await context.SaveChangesAsync();

        var controller = new AdminController(authService, reservaService)
        {
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), new MockTempDataProvider())
        };

        // 2. Crear cita manual desde el dashboard
        var crearModel = new CrearReservaManualViewModel
        {
            Nombre = "Valeria Ramos",
            Telefono = "988776655",
            ServicioId = servicio.Id,
            Fecha = new DateOnly(2026, 10, 24),
            Horario = "Tarde 1 (13:00 a 16:30)",
            Precio = 180m,
            MontoAbonado = 0m,
            Notas = "Prueba de mecha requerida",
            EstadoInicial = "Pendiente"
        };

        var postResult = await controller.CrearReservaManual(crearModel);
        var redirectResult = Assert.IsType<RedirectToActionResult>(postResult);
        Assert.Equal(nameof(AdminController.Index), redirectResult.ActionName);

        var citaCreada = await context.Reservas.FirstOrDefaultAsync(r => r.Telefono == "988776655");
        Assert.NotNull(citaCreada);
        Assert.Equal("Valeria Ramos", citaCreada.Nombre);
        Assert.Equal("Pendiente", citaCreada.Estado);
        Assert.Equal(0m, citaCreada.MontoAbonado);

        // 3. Cambiar estado a Confirmada vía endpoint AJAX (debe auto-asignar abono de S/. 20)
        var estadoReq = new ActualizarEstadoRequest
        {
            Id = citaCreada.Id,
            Estado = "🟢 Reserva Confirmada"
        };
        var estadoResult = await controller.ActualizarEstado(estadoReq);
        var estadoJson = Assert.IsType<JsonResult>(estadoResult);
        Assert.NotNull(estadoJson.Value);

        var citaConfirmada = await context.Reservas.FindAsync(citaCreada.Id);
        Assert.Equal("Confirmada", citaConfirmada!.Estado);
        Assert.Equal(20.00m, citaConfirmada.MontoAbonado);

        // 4. Actualizar notas in situ vía endpoint AJAX
        var notasReq = new ActualizarNotasRequest
        {
            Id = citaCreada.Id,
            Notas = "Abonó S/. 20 Yape op 48291. Tono cenizo seleccionado."
        };
        var notasResult = await controller.ActualizarNotas(notasReq);
        var notasJson = Assert.IsType<JsonResult>(notasResult);
        Assert.NotNull(notasJson.Value);

        var citaConNota = await context.Reservas.FindAsync(citaCreada.Id);
        Assert.Contains("op 48291", citaConNota!.Notas);

        // 5. Verificar visualización en el Index del Dashboard para esa semana
        var indexResult = await controller.Index(fecha: new DateOnly(2026, 10, 24), busqueda: null, estado: null);
        var viewResult = Assert.IsType<ViewResult>(indexResult);
        var dashboardVm = Assert.IsType<AdminDashboardViewModel>(viewResult.Model);

        Assert.Contains(dashboardVm.ReservasSemana, r => r.Id == citaCreada.Id);
        Assert.Contains(dashboardVm.ReservasTabla, r => r.Id == citaCreada.Id);

        // 6. Probar filtrado en tabla
        var filtroBusquedaResult = await controller.Index(fecha: null, busqueda: "Valeria", estado: null);
        var viewFiltro = Assert.IsType<ViewResult>(filtroBusquedaResult);
        var vmFiltro = Assert.IsType<AdminDashboardViewModel>(viewFiltro.Model);
        Assert.Single(vmFiltro.ReservasTabla);
        Assert.Equal("Valeria Ramos", vmFiltro.ReservasTabla[0].Nombre);

        // Filtro que no coincide
        var filtroVacioResult = await controller.Index(fecha: null, busqueda: "Inexistente", estado: null);
        var viewVacio = Assert.IsType<ViewResult>(filtroVacioResult);
        var vmVacio = Assert.IsType<AdminDashboardViewModel>(viewVacio.Model);
        Assert.Empty(vmVacio.ReservasTabla);
    }

    private class MockTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();
        public IDictionary<string, object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context) => _data;
        public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context, IDictionary<string, object> values) { }
    }
}
