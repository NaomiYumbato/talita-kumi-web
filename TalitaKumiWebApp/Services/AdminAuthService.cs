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
                NombreCompleto = defaultNombre.Trim(),
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            admin.PasswordHash = _passwordHasher.HashPassword(admin, defaultPassword.Trim());

            _context.UsuariosAdmin.Add(admin);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Si la contraseña provista por variable de entorno es diferente a la hash actual, se actualiza automáticamente
            var check = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, defaultPassword.Trim());
            if (check == PasswordVerificationResult.Failed)
            {
                admin.PasswordHash = _passwordHasher.HashPassword(admin, defaultPassword.Trim());
                if (!string.IsNullOrWhiteSpace(defaultNombre))
                {
                    admin.NombreCompleto = defaultNombre.Trim();
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
