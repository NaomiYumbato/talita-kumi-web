using System.ComponentModel.DataAnnotations;

namespace TalitaKumiWebApp.ViewModels.Admin;

public class LoginViewModel
{
    [Required(ErrorMessage = "El usuario es obligatorio")]
    [Display(Name = "Usuario")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordar sesión")]
    public bool RememberMe { get; set; } = true;
}
