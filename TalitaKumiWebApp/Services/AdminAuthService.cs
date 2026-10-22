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

    public async Task EnsureDefaultAdminUserAsync(string defaultUsername, string defaultPassword, string defaultNombre)
    {
        var existe = await _context.UsuariosAdmin.AnyAsync();
        if (!existe)
        {
            var admin = new UsuarioAdmin
            {
                Username = defaultUsername.Trim(),
                NombreCompleto = defaultNombre.Trim(),
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            admin.PasswordHash = _passwordHasher.HashPassword(admin, defaultPassword);

            _context.UsuariosAdmin.Add(admin);
            await _context.SaveChangesAsync();
        }
    }
}
