using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TalitaKumiWebApp.Models.Cotizaciones;

public class CotizacionUñaRegistro
{
    public int Id { get; set; }

    public int ReservaId { get; set; }

    [JsonIgnore]
    public Reserva? Reserva { get; set; }

    [Required]
    [MaxLength(50)]
    public string TecnicaBase { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Largo { get; set; } = string.Empty;

    [MaxLength(50)]
    public string NailArt { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Retiro { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubtotalTecnica { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubtotalLargo { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubtotalArte { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SubtotalRetiro { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecioTotalCalculado { get; set; }

    public int DuracionMinutosCalculada { get; set; }

    public DateTime FechaCotizacion { get; set; } = DateTime.UtcNow;
}
