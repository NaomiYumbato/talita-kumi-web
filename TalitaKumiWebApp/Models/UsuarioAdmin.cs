using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalitaKumiWebApp.Models;

[Table("UsuariosAdmin")]
public class UsuarioAdmin
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NombreCompleto { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime? UltimoAcceso { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
