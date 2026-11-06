using Microsoft.EntityFrameworkCore;
using TalitaKumiWebApp.Data;
using TalitaKumiWebApp.Services;
using Xunit;

namespace TalitaKumiWebApp.Tests;

public class AdminAuthServiceTests
{
    [Fact]
    public async Task AdminAuthService_ValidarCredenciales_RetornaTrueParaPasswordCorrecto_YFalseParaIncorrecto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new AdminAuthService(context);

        await service.EnsureDefaultAdminUserAsync("naomi", "TalitaKumi2026*", "Naomi Yumbato");

        var ok = await service.ValidateCredentialsAsync("naomi", "TalitaKumi2026*");
        var errorClave = await service.ValidateCredentialsAsync("naomi", "ClaveErronea");
        var errorUser = await service.ValidateCredentialsAsync("desconocido", "TalitaKumi2026*");

        Assert.True(ok);
        Assert.False(errorClave);
        Assert.False(errorUser);

        var usuario = await service.GetByUsernameAsync("naomi");
        Assert.NotNull(usuario);
        Assert.Equal("Naomi Yumbato", usuario.NombreCompleto);
        Assert.NotNull(usuario.UltimoAcceso);
    }

    [Fact]
    public async Task AdminAuthService_UpdatePassword_ActualizaPasswordHashYValidaConNuevaClave()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new AdminAuthService(context);

        await service.EnsureDefaultAdminUserAsync("naomi", "ClaveInicial123", "Naomi");

        var actualizado = await service.UpdatePasswordAsync("naomi", "NuevaClaveSecreta456");
        Assert.True(actualizado);

        var okVieja = await service.ValidateCredentialsAsync("naomi", "ClaveInicial123");
        var okNueva = await service.ValidateCredentialsAsync("naomi", "NuevaClaveSecreta456");

        Assert.False(okVieja);
        Assert.True(okNueva);
    }

    [Fact]
    public async Task AdminAuthService_EnsureDefaultAdminUser_SiUsuarioYaExiste_ConservaDatosYNoSobrescribe()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new AdminAuthService(context);

        await service.EnsureDefaultAdminUserAsync("admin", "ClaveModificadaPorUsuario", "Mi Nombre Personalizado");
        Assert.True(await service.ValidateCredentialsAsync("admin", "ClaveModificadaPorUsuario"));

        // Se re-ejecuta al reiniciar la app con las credenciales por defecto iniciales
        await service.EnsureDefaultAdminUserAsync("admin", "ClaveInicialDeEntorno", "NombrePorDefecto");

        // Los datos del usuario en la base de datos se deben conservar intactos
        Assert.True(await service.ValidateCredentialsAsync("admin", "ClaveModificadaPorUsuario"));
        Assert.False(await service.ValidateCredentialsAsync("admin", "ClaveInicialDeEntorno"));

        var usuario = await service.GetByUsernameAsync("admin");
        Assert.Equal("Mi Nombre Personalizado", usuario?.NombreCompleto);
    }

    [Fact]
    public async Task AdminAuthService_ActualizarPerfil_SoloNombre_ActualizaCorrectamente()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new AdminAuthService(context);

        await service.EnsureDefaultAdminUserAsync("naomi", "Pass123456", "Naomi Original");

        var (success, message, finalUser) = await service.ActualizarPerfilAsync("naomi", "naomi", "Naomi Yumbato Actualizada", null, null);

        Assert.True(success);
        Assert.Equal("naomi", finalUser);
        var user = await service.GetByUsernameAsync("naomi");
        Assert.NotNull(user);
        Assert.Equal("Naomi Yumbato Actualizada", user.NombreCompleto);
        Assert.True(await service.ValidateCredentialsAsync("naomi", "Pass123456"));
    }

    [Fact]
    public async Task AdminAuthService_ActualizarPerfil_NombreYPassword_ActualizaAmbos()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new AdminAuthService(context);

        await service.EnsureDefaultAdminUserAsync("naomi", "PassOriginal123", "Naomi");

        var (success, _, finalUser) = await service.ActualizarPerfilAsync("naomi", "naomi", "Naomi Y.", "PassOriginal123", "NuevaSuperClave789");

        Assert.True(success);
        Assert.Equal("naomi", finalUser);
        var user = await service.GetByUsernameAsync("naomi");
        Assert.NotNull(user);
        Assert.Equal("Naomi Y.", user.NombreCompleto);
        Assert.False(await service.ValidateCredentialsAsync("naomi", "PassOriginal123"));
        Assert.True(await service.ValidateCredentialsAsync("naomi", "NuevaSuperClave789"));
    }

    [Fact]
    public async Task AdminAuthService_ActualizarPerfil_CambioDeUsername_ActualizaCorrectamenteYPermiteLoginConNuevoUsername()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new AdminAuthService(context);

        await service.EnsureDefaultAdminUserAsync("usuario_viejo", "MiClave123", "Estilista Principal");

        var (success, _, finalUser) = await service.ActualizarPerfilAsync("usuario_viejo", "usuario_nuevo", "Estilista Principal Renombrada", null, null);

        Assert.True(success);
        Assert.Equal("usuario_nuevo", finalUser);

        // Ya no debe existir ni permitir login con el usuario viejo
        var userViejo = await service.GetByUsernameAsync("usuario_viejo");
        Assert.Null(userViejo);
        Assert.False(await service.ValidateCredentialsAsync("usuario_viejo", "MiClave123"));

        // Debe permitir login con el usuario nuevo
        var userNuevo = await service.GetByUsernameAsync("usuario_nuevo");
        Assert.NotNull(userNuevo);
        Assert.Equal("Estilista Principal Renombrada", userNuevo.NombreCompleto);
        Assert.True(await service.ValidateCredentialsAsync("usuario_nuevo", "MiClave123"));
    }

    [Fact]
    public async Task AdminAuthService_ActualizarPerfil_UsernameYaOcupado_RetornaError()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new AdminAuthService(context);

        await service.EnsureDefaultAdminUserAsync("admin1", "Pass123456", "Admin Uno");
        await service.EnsureDefaultAdminUserAsync("admin2", "Pass123456", "Admin Dos");

        // admin1 intenta cambiar su username a admin2
        var (success, message, _) = await service.ActualizarPerfilAsync("admin1", "admin2", "Admin Uno", null, null);

        Assert.False(success);
        Assert.Contains("en uso", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminAuthService_ActualizarPerfil_PasswordActualIncorrecto_RetornaError()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new AdminAuthService(context);

        await service.EnsureDefaultAdminUserAsync("naomi", "PassOriginal123", "Naomi");

        var (success, message, _) = await service.ActualizarPerfilAsync("naomi", "naomi", "Naomi Y.", "ClaveEquivocada", "NuevaClave123");

        Assert.False(success);
        Assert.Contains("actual", message, StringComparison.OrdinalIgnoreCase);
        // La contraseña no debió haber cambiado
        Assert.True(await service.ValidateCredentialsAsync("naomi", "PassOriginal123"));
    }

    [Fact]
    public async Task AdminAuthService_ActualizarPerfil_PasswordNuevoMuyCorto_RetornaError()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new AdminAuthService(context);

        await service.EnsureDefaultAdminUserAsync("naomi", "PassOriginal123", "Naomi");

        var (success, message, _) = await service.ActualizarPerfilAsync("naomi", "naomi", "Naomi Y.", "PassOriginal123", "12345");

        Assert.False(success);
        Assert.Contains("6 caracteres", message);
    }
}
