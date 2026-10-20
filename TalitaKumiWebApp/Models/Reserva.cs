using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TalitaKumiWebApp.Models.Cotizaciones;

namespace TalitaKumiWebApp.Models;

public class Reserva
{
    public int Id { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int? ServicioId { get; set; }
    public Servicio? ServicioRelacionado { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono / WhatsApp es obligatorio")]
    [MaxLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Servicio { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecioEstimado { get; set; }

    public int DuracionMinutos { get; set; }

    [Required]
    public DateOnly Fecha { get; set; }

    [Required]
    [MaxLength(50)]
    public string Horario { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notas { get; set; }

    [MaxLength(30)]
    public string Estado { get; set; } = "Pendiente";

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public CotizacionUñaRegistro? CotizacionUña { get; set; }
}
