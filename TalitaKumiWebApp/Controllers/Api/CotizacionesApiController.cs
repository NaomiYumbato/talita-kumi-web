using Microsoft.AspNetCore.Mvc;
using TalitaKumiWebApp.Models.Cotizaciones;
using TalitaKumiWebApp.Services;

namespace TalitaKumiWebApp.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class CotizacionesController : ControllerBase
{
    private readonly INailPricingEngine _pricingEngine;

    public CotizacionesController(INailPricingEngine pricingEngine)
    {
        _pricingEngine = pricingEngine;
    }

    [HttpPost("unas")]
    public ActionResult<CotizacionUñaResult> CotizarUñas([FromBody] CotizacionUñaRequest request)
    {
        var resultado = _pricingEngine.Calcular(request ?? new CotizacionUñaRequest());
        return Ok(resultado);
    }
}
