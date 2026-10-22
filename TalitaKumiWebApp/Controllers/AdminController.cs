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
    public async Task<IActionResult> Index(DateOnly? fecha, string? busqueda, string? estado)
    {
        var fechaBase = fecha ?? DateOnly.FromDateTime(DateTime.Today);
        int delta = (int)fechaBase.DayOfWeek;
        if (delta == 0) delta = 7;
        var inicioSemana = fechaBase.AddDays(1 - delta);
        var finSemana = inicioSemana.AddDays(6);

        var reservasSemana = await _reservaService.ObtenerReservasSemanaAsync(inicioSemana, finSemana);
        var reservasTabla = await _reservaService.ObtenerReservasFiltradasAsync(fecha, busqueda, estado);
        var servicios = await _reservaService.ObtenerServiciosActivosAsync();

        var vm = new AdminDashboardViewModel
        {
            FechaSeleccionada = fechaBase,
            InicioSemana = inicioSemana,
            FinSemana = finSemana,
            ReservasSemana = reservasSemana,
            ReservasTabla = reservasTabla,
            ServiciosDisponibles = servicios,
            FiltroBusqueda = busqueda,
            FiltroEstado = estado
        };

        return View(vm);
    }

    [HttpPost("Admin/ActualizarEstado")]
    public async Task<IActionResult> ActualizarEstado([FromBody] ActualizarEstadoRequest req)
    {
        if (req == null || req.Id <= 0 || string.IsNullOrWhiteSpace(req.Estado))
        {
            return BadRequest(new { success = false, message = "Datos inválidos." });
        }

        var ok = await _reservaService.ActualizarEstadoAsync(req.Id, req.Estado);
        if (!ok)
        {
            return NotFound(new { success = false, message = "Reserva no encontrada." });
        }

        var reserva = await _reservaService.ObtenerPorIdAsync(req.Id);
        return Json(new
        {
            success = true,
            estado = reserva?.Estado,
            montoAbonado = reserva?.MontoAbonado ?? 0m
        });
    }

    [HttpPost("Admin/ActualizarNotas")]
    public async Task<IActionResult> ActualizarNotas([FromBody] ActualizarNotasRequest req)
    {
        if (req == null || req.Id <= 0)
        {
            return BadRequest(new { success = false, message = "Datos inválidos." });
        }

        var ok = await _reservaService.ActualizarNotasAsync(req.Id, req.Notas ?? string.Empty);
        if (!ok)
        {
            return NotFound(new { success = false, message = "Reserva no encontrada." });
        }

        return Json(new
        {
            success = true,
            notas = req.Notas ?? string.Empty
        });
    }

    [HttpPost("Admin/CrearReservaManual")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearReservaManual(CrearReservaManualViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Por favor verifica los campos de la reserva.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _reservaService.CrearReservaManualAsync(model);
            TempData["Mensaje"] = $"Cita agendada con éxito para {model.Nombre}.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al agendar cita: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
