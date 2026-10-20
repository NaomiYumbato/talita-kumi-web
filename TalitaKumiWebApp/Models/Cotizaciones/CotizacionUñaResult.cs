namespace TalitaKumiWebApp.Models.Cotizaciones;

public class CotizacionUñaResult
{
    public decimal PrecioTotal { get; set; }
    public int DuracionMinutos { get; set; }
    public string PrecioFormateado => $"S/. {PrecioTotal:0.00}";
    public string DuracionFormateada
    {
        get
        {
            var horas = DuracionMinutos / 60;
            var mins = DuracionMinutos % 60;
            return horas > 0 ? $"{horas}h {mins:D2}m" : $"{mins}m";
        }
    }
    public string ResumenServicio { get; set; } = string.Empty;
    public List<string> Detalles { get; set; } = new();
}
