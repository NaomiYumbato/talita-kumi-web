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
}
