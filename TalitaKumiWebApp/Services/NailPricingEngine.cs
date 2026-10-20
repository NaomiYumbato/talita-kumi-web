using TalitaKumiWebApp.Models.Cotizaciones;

namespace TalitaKumiWebApp.Services;

public interface INailPricingEngine
{
    CotizacionUñaResult Calcular(CotizacionUñaRequest request);
}

public class NailPricingEngine : INailPricingEngine
{
    public CotizacionUñaResult Calcular(CotizacionUñaRequest request)
    {
        decimal precio = 0m;
        int duracion = 0;
        var detalles = new List<string>();

        // 1. Técnica Base
        switch (request.Tecnica?.Trim().ToLowerInvariant())
        {
            case "kapping":
                precio += 65m;
                duracion += 75;
                detalles.Add("Kapping Gel / Acrigel: S/. 65.00");
                break;
            case "soft gel":
            case "softgel":
                precio += 90m;
                duracion += 90;
                detalles.Add("Soft Gel Tips: S/. 90.00");
                break;
            case "esculpidas":
                precio += 120m;
                duracion += 120;
                detalles.Add("Uñas Esculpidas: S/. 120.00");
                break;
            case "semipermanente":
            default:
                precio += 45m;
                duracion += 60;
                detalles.Add("Manicura Semipermanente: S/. 45.00");
                break;
        }

        // 2. Largo
        switch (request.Largo?.Trim().ToLowerInvariant())
        {
            case "medio":
            case "1-2":
                precio += 15m;
                duracion += 15;
                detalles.Add("Largo Medio (N° 1-2): +S/. 15.00");
                break;
            case "xl":
            case "largo":
            case "3-5":
                precio += 30m;
                duracion += 30;
                detalles.Add("Largo Extremo XL: +S/. 30.00");
                break;
            case "natural":
            case "corto":
            default:
                // Sin recargo
                break;
        }

        // 3. Nail Art / Diseño
        switch (request.NailArt?.Trim().ToLowerInvariant())
        {
            case "francesita":
            case "simple":
            case "cat eye":
                precio += 15m;
                duracion += 20;
                detalles.Add("Diseño Básico / French: +S/. 15.00");
                break;
            case "full arte":
            case "complejo":
            case "3d":
                precio += 35m;
                duracion += 40;
                detalles.Add("Full Nail Art Avanzado: +S/. 35.00");
                break;
            case "liso":
            default:
                // Sin recargo
                break;
        }

        // 4. Retiro
        switch (request.Retiro?.Trim().ToLowerInvariant())
        {
            case "semipermanente":
                precio += 10m;
                duracion += 20;
                detalles.Add("Retiro Semipermanente: +S/. 10.00");
                break;
            case "esculpidas":
            case "soft gel":
            case "softgel":
                precio += 20m;
                duracion += 30;
                detalles.Add("Retiro Esculpidas / Soft Gel: +S/. 20.00");
                break;
            case "ninguno":
            default:
                // Sin recargo
                break;
        }

        var resumen = $"{request.Tecnica} (Largo {request.Largo}, Arte {request.NailArt})";

        return new CotizacionUñaResult
        {
            PrecioTotal = precio,
            DuracionMinutos = duracion,
            ResumenServicio = resumen,
            Detalles = detalles
        };
    }
}
