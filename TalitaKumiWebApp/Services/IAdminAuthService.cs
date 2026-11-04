using TalitaKumiWebApp.Models;

namespace TalitaKumiWebApp.Services;

public interface IAdminAuthService
{
    Task<bool> ValidateCredentialsAsync(string username, string password);
    Task<UsuarioAdmin?> GetByUsernameAsync(string username);
    Task<bool> UpdatePasswordAsync(string username, string newPassword);
    Task EnsureDefaultAdminUserAsync(string defaultUsername, string defaultPassword, string defaultNombre);
}
