using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalitaKumiWebApp.Controllers;
using TalitaKumiWebApp.Data;
using TalitaKumiWebApp.Services;
using TalitaKumiWebApp.ViewModels.Admin;
using Xunit;

namespace TalitaKumiWebApp.Tests;

public class AdminControllerAuthTests
{
    [Fact]
    public async Task Login_POST_ConCredencialesInvalidas_RetornaVistaConError()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var authService = new AdminAuthService(context);
        await authService.EnsureDefaultAdminUserAsync("naomi", "TalitaKumi2026*", "Naomi Yumbato");

        var controller = new AdminController(authService, null!);

        var model = new LoginViewModel { Username = "naomi", Password = "ClaveErronea" };
        var result = await controller.Login(model, returnUrl: null);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(string.Empty));
    }

    [Fact]
    public void Login_GET_RetornaVistaConModelVacio()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new AppDbContext(options);
        var authService = new AdminAuthService(context);
        var controller = new AdminController(authService, null!);

        var result = controller.Login(returnUrl: null);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.Model);
    }
}
