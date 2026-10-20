using TalitaKumiWebApp.Models.Cotizaciones;
using TalitaKumiWebApp.Services;
using Xunit;

namespace TalitaKumiWebApp.Tests;

public class NailPricingEngineTests
{
    private readonly NailPricingEngine _engine = new();

    [Fact]
    public void Calcular_SemipermanenteBasico_Retorna45SolesY60Minutos()
    {
        // Arrange
        var request = new CotizacionUñaRequest
        {
            Tecnica = "Semipermanente",
            Largo = "Natural",
            NailArt = "Liso",
            Retiro = "Ninguno"
        };

        // Act
        var result = _engine.Calcular(request);

        // Assert
        Assert.Equal(45m, result.PrecioTotal);
        Assert.Equal(60, result.DuracionMinutos);
        Assert.Equal("S/. 45.00", result.PrecioFormateado);
        Assert.Equal("1h 00m", result.DuracionFormateada);
    }

    [Fact]
    public void Calcular_SoftGelMedioConFrancesita_Retorna120SolesY125Minutos()
    {
        // Arrange: Soft Gel (90) + Largo Medio (15) + Francesita (15) + Sin Retiro (0) = 120
        // Minutos: 90 + 15 + 20 = 125m (2h 05m)
        var request = new CotizacionUñaRequest
        {
            Tecnica = "Soft Gel",
            Largo = "Medio",
            NailArt = "Francesita",
            Retiro = "Ninguno"
        };

        // Act
        var result = _engine.Calcular(request);

        // Assert
        Assert.Equal(120m, result.PrecioTotal);
        Assert.Equal(125, result.DuracionMinutos);
        Assert.Equal("S/. 120.00", result.PrecioFormateado);
        Assert.Equal("2h 05m", result.DuracionFormateada);
    }

    [Fact]
    public void Calcular_EsculpidasXLConFullArteYRetiro_Retorna205SolesY210Minutos()
    {
        // Arrange: Esculpidas (120) + Largo XL (30) + Full Arte (35) + Retiro Esculpidas (20) = 205
        // Minutos: 120 + 30 + 40 + 30 = 220m (3h 40m)
        var request = new CotizacionUñaRequest
        {
            Tecnica = "Esculpidas",
            Largo = "XL",
            NailArt = "Full Arte",
            Retiro = "Esculpidas"
        };

        // Act
        var result = _engine.Calcular(request);

        // Assert
        Assert.Equal(205m, result.PrecioTotal);
        Assert.Equal(220, result.DuracionMinutos);
        Assert.Equal("S/. 205.00", result.PrecioFormateado);
        Assert.Equal("3h 40m", result.DuracionFormateada);
    }

    [Fact]
    public void Calcular_KappingGelConRetiroSemi_Retorna75SolesY95Minutos()
    {
        // Arrange: Kapping (65) + Natural (0) + Liso (0) + Retiro Semi (10) = 75
        // Minutos: 75 + 0 + 0 + 20 = 95m (1h 35m)
        var request = new CotizacionUñaRequest
        {
            Tecnica = "Kapping",
            Largo = "Natural",
            NailArt = "Liso",
            Retiro = "Semipermanente"
        };

        // Act
        var result = _engine.Calcular(request);

        // Assert
        Assert.Equal(75m, result.PrecioTotal);
        Assert.Equal(95, result.DuracionMinutos);
        Assert.Equal("S/. 75.00", result.PrecioFormateado);
        Assert.Equal("1h 35m", result.DuracionFormateada);
    }
}
