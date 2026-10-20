using Microsoft.AspNetCore.Mvc;
using TalitaKumiWebApp.Models;
using TalitaKumiWebApp.Services;

namespace TalitaKumiWebApp.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ReservasController : ControllerBase
{
    private readonly IReservaService _reservaService;

    public ReservasController(IReservaService reservaService)
    {
        _reservaService = reservaService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Reserva>>> ObtenerTodas()
    {
        var lista = await _reservaService.ObtenerReservasAsync();
        return Ok(lista);
    }

    [HttpPost]
    public async Task<ActionResult<Reserva>> CrearReserva([FromBody] Reserva reserva)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var guardada = await _reservaService.CrearReservaAsync(reserva);
            return CreatedAtAction(nameof(ObtenerTodas), new { id = guardada.Id }, guardada);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
