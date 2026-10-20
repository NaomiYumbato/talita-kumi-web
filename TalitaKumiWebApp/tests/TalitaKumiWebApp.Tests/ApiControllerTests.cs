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
    }
}
