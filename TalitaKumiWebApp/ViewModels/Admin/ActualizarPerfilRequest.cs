using System.ComponentModel.DataAnnotations;

namespace TalitaKumiWebApp.ViewModels.Admin;

public class ActualizarPerfilRequest
{
    [Required(ErrorMessage = "El usuario de acceso es requerido.")]
    [MaxLength(50)]
    public string NuevoUsername { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es requerido.")]
    [MaxLength(100)]
    public string NombreCompleto { get; set; } = string.Empty;

    public string? PasswordActual { get; set; }

    [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
    public string? NuevoPassword { get; set; }

    public string? ConfirmarPassword { get; set; }
}
