using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalitaKumiWebApp.Services;
using TalitaKumiWebApp.ViewModels.Admin;

namespace TalitaKumiWebApp.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly IAdminAuthService _authService;
    private readonly IReservaService _reservaService;

    public AdminController(IAdminAuthService authService, IReservaService reservaService)
    {
        _authService = authService;
        _reservaService = reservaService;
    }

    [AllowAnonymous]
    [HttpGet("Admin/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User?.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost("Admin/Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var esValido = await _authService.ValidateCredentialsAsync(model.Username, model.Password);
        if (!esValido)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(model);
        }

        var usuario = await _authService.GetByUsernameAsync(model.Username);
        var nombreDisplay = usuario?.NombreCompleto ?? model.Username;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, model.Username),
            new Claim(ClaimTypes.GivenName, nombreDisplay),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
        };

        if (HttpContext != null)
        {
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Admin/Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (HttpContext != null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("Admin")]
    [HttpGet("Admin/Index")]
    public IActionResult Index()
    {
        return View();
    }
}
