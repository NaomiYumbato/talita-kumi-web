using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TalitaKumiWebApp.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono / WhatsApp es obligatorio")]
    [MaxLength(30)]
    public string Telefono { get; set; } = string.Empty;

    [MaxLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public List<Reserva> Reservas { get; set; } = new();
}
