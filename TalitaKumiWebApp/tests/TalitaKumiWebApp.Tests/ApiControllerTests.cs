using Microsoft.AspNetCore.Mvc;
using TalitaKumiWebApp.Controllers.Api;
using TalitaKumiWebApp.Models;
using TalitaKumiWebApp.Models.Cotizaciones;
using TalitaKumiWebApp.Services;
using Xunit;

namespace TalitaKumiWebApp.Tests;

public class ApiControllerTests
{
    [Fact]
    public void CotizacionesController_CotizarUñas_Retorna200OkConResultado()
    {
        // Arrange
        var engine = new NailPricingEngine();
        var controller = new CotizacionesController(engine);
        var request = new CotizacionUñaRequest
        {
            Tecnica = "Soft Gel",
            Largo = "Medio",
            NailArt = "Francesita",
            Retiro = "Ninguno"
        };

        // Act
        var actionResult = controller.CotizarUñas(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<CotizacionUñaResult>(okResult.Value);
        Assert.Equal(120m, response.PrecioTotal);
        Assert.Equal("S/. 120.00", response.PrecioFormateado);
    }

    [Fact]
    public async Task ReservasController_CrearReservaValida_Retorna201CreatedAtAction()
    {
        // Arrange
        var mockService = new FakeReservaService();
        var controller = new ReservasController(mockService);
        var reserva = new Reserva
        {
            Nombre = "Andrea Gomez",
            Telefono = "999888777",
            Servicio = "Lifting de Pestañas",
            PrecioEstimado = 70m,
            DuracionMinutos = 60,
            Fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Horario = "Mañana (10:00 - 13:00)"
        };

        // Act
        var actionResult = await controller.CrearReserva(reserva);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var response = Assert.IsType<Reserva>(createdResult.Value);
        Assert.Equal("Andrea Gomez", response.Nombre);
        Assert.True(response.Id > 0);
    }

    [Fact]
    public async Task ReservasController_ObtenerDisponibilidad_Retorna200OkConDatos()
    {
        var mockService = new FakeReservaService();
        var controller = new ReservasController(mockService);
        var fecha = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        var actionResult = await controller.ObtenerDisponibilidad(fecha);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dto = Assert.IsType<DisponibilidadFechaDto>(okResult.Value);
        Assert.Equal(fecha, dto.Fecha);
    }

    [Fact]
    public async Task ReservasController_ObtenerFechasOcupadas_Retorna200OkConLista()
    {
        var mockService = new FakeReservaService();
        var controller = new ReservasController(mockService);

        var actionResult = await controller.ObtenerFechasOcupadas(null, null);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var lista = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
        Assert.NotNull(lista);
    }

    private class FakeReservaService : IReservaService
    {
        private int _counter = 1;
        public Task<Reserva> CrearReservaAsync(Reserva reserva)
        {
            reserva.Id = _counter++;
            return Task.FromResult(reserva);
        }

        public Task<List<Reserva>> ObtenerReservasAsync() => Task.FromResult(new List<Reserva>());
        public Task<Reserva?> ObtenerPorIdAsync(int id) => Task.FromResult<Reserva?>(null);

        public Task<List<Reserva>> ObtenerReservasSemanaAsync(DateOnly inicioSemana, DateOnly finSemana) => Task.FromResult(new List<Reserva>());
        public Task<List<Reserva>> ObtenerReservasFiltradasAsync(DateOnly? fecha, string? busqueda, string? estado) => Task.FromResult(new List<Reserva>());
        public Task<bool> ActualizarEstadoAsync(int id, string nuevoEstado) => Task.FromResult(true);
        public Task<bool> ActualizarNotasAsync(int id, string notas) => Task.FromResult(true);
        public Task<Reserva> CrearReservaManualAsync(TalitaKumiWebApp.ViewModels.Admin.CrearReservaManualViewModel model) => Task.FromResult(new Reserva());
        public Task<List<Servicio>> ObtenerServiciosActivosAsync() => Task.FromResult(new List<Servicio>());
        public Task<DisponibilidadFechaDto> ObtenerDisponibilidadFechaAsync(DateOnly fecha) => Task.FromResult(new DisponibilidadFechaDto { Fecha = fecha });
        public Task<List<DateOnly>> ObtenerFechasCompletamenteOcupadasAsync(DateOnly desde, DateOnly hasta) => Task.FromResult(new List<DateOnly>());
    }
}
