using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TalitaKumiWebApp.Data;
using TalitaKumiWebApp.Models;

namespace TalitaKumiWebApp.Services;

public class AdminAuthService : IAdminAuthService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<UsuarioAdmin> _passwordHasher;

    public AdminAuthService(AppDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<UsuarioAdmin>();
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var admin = await _context.UsuariosAdmin
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.Trim().ToLower() && u.Activo);

        if (admin == null)
        {
            return false;
        }

        var result = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, password);
        if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            admin.UltimoAcceso = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<UsuarioAdmin?> GetByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;

        return await _context.UsuariosAdmin
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.Trim().ToLower());
    }

    public async Task<bool> UpdatePasswordAsync(string username, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(newPassword))
        {
            return false;
        }

        var admin = await _context.UsuariosAdmin
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.Trim().ToLower());

        if (admin == null)
        {
            return false;
        }

        admin.PasswordHash = _passwordHasher.HashPassword(admin, newPassword.Trim());
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task EnsureDefaultAdminUserAsync(string defaultUsername, string defaultPassword, string defaultNombre)
    {
        if (string.IsNullOrWhiteSpace(defaultUsername) || string.IsNullOrWhiteSpace(defaultPassword))
        {
            return;
        }

        var admin = await _context.UsuariosAdmin
            .FirstOrDefaultAsync(u => u.Username.ToLower() == defaultUsername.Trim().ToLower());

        if (admin == null)
        {
            admin = new UsuarioAdmin
            {
                Username = defaultUsername.Trim(),
                NombreCompleto = (string.IsNullOrWhiteSpace(defaultNombre) ? defaultUsername : defaultNombre).Trim(),
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            admin.PasswordHash = _passwordHasher.HashPassword(admin, defaultPassword.Trim());

            _context.UsuariosAdmin.Add(admin);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<(bool success, string message, string? finalUsername)> ActualizarPerfilAsync(
        string currentUsername,
        string nuevoUsername,
        string nuevoNombre,
        string? passwordActual,
        string? nuevoPassword)
    {
        if (string.IsNullOrWhiteSpace(currentUsername))
        {
            return (false, "Identificador de usuario actual inválido.", null);
        }

        if (string.IsNullOrWhiteSpace(nuevoUsername))
        {
            return (false, "El usuario de acceso es requerido.", null);
        }

        if (string.IsNullOrWhiteSpace(nuevoNombre))
        {
            return (false, "El nombre completo es requerido.", null);
        }

        var admin = await _context.UsuariosAdmin
            .FirstOrDefaultAsync(u => u.Username.ToLower() == currentUsername.Trim().ToLower());

        if (admin == null)
        {
            return (false, "Usuario no encontrado.", null);
        }

        var trimmedNuevoUsername = nuevoUsername.Trim();

        // Si se cambia el nombre de usuario, verificar que no esté ocupado por otro registro
        if (!string.Equals(admin.Username, trimmedNuevoUsername, StringComparison.OrdinalIgnoreCase))
        {
            var existe = await _context.UsuariosAdmin
                .AnyAsync(u => u.Username.ToLower() == trimmedNuevoUsername.ToLower() && u.Id != admin.Id);

            if (existe)
            {
                return (false, "El usuario de acceso ingresado ya está en uso. Por favor elige otro.", null);
            }

            admin.Username = trimmedNuevoUsername;
        }

        // Si se solicitó cambio de contraseña
        if (!string.IsNullOrWhiteSpace(nuevoPassword))
        {
            if (string.IsNullOrWhiteSpace(passwordActual))
            {
                return (false, "Debes ingresar tu contraseña actual para autorizar el cambio.", null);
            }

            var verification = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, passwordActual);
            if (verification == PasswordVerificationResult.Failed)
            {
                return (false, "La contraseña actual ingresada es incorrecta.", null);
            }

            if (nuevoPassword.Trim().Length < 6)
            {
                return (false, "La nueva contraseña debe tener al menos 6 caracteres.", null);
            }

            admin.PasswordHash = _passwordHasher.HashPassword(admin, nuevoPassword.Trim());
        }

        admin.NombreCompleto = nuevoNombre.Trim();
        await _context.SaveChangesAsync();

        return (true, "Perfil actualizado correctamente.", admin.Username);
    }
}
