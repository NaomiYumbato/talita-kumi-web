using System.ComponentModel.DataAnnotations;

namespace TalitaKumiWebApp.ViewModels.Admin;

public class CrearReservaManualViewModel
{
    [Required(ErrorMessage = "El nombre de la clienta es obligatorio")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono o WhatsApp es obligatorio")]
    [MaxLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar un servicio")]
    public int ServicioId { get; set; }

    [Required(ErrorMessage = "La fecha de la cita es obligatoria")]
    public DateOnly Fecha { get; set; }

    [Required(ErrorMessage = "El turno u horario es obligatorio")]
    [MaxLength(50)]
    public string Horario { get; set; } = string.Empty;

    public decimal? Precio { get; set; }

    public decimal? MontoAbonado { get; set; }

    [MaxLength(500)]
    public string? Notas { get; set; }

    public string EstadoInicial { get; set; } = "Confirmada";
}
