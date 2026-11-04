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
    public async Task AdminAuthService_EnsureDefaultAdminUser_SiClaveCambia_ActualizaHashAutomaticamente()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var service = new AdminAuthService(context);

        await service.EnsureDefaultAdminUserAsync("naomi", "ClaveVersion1", "Naomi");
        Assert.True(await service.ValidateCredentialsAsync("naomi", "ClaveVersion1"));

        // Se re-ejecuta con una nueva contraseña secreta de entorno
        await service.EnsureDefaultAdminUserAsync("naomi", "ClaveVersion2Secreta", "Naomi");

        Assert.False(await service.ValidateCredentialsAsync("naomi", "ClaveVersion1"));
        Assert.True(await service.ValidateCredentialsAsync("naomi", "ClaveVersion2Secreta"));
    }
}
