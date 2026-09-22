using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TrabajosWeb.Shared.DTOs;
using TrabajosWeb.Web.Services;

namespace TrabajosWeb.Web.Controllers;

public class CuentaController : Controller
{
    /// <summary>
    /// Cuánto dura la sesión sin actividad. Se reemite en cada renovación
    /// desde sesion-inactividad.js, así que en la práctica dura mientras
    /// haya uso real, no 60 minutos fijos desde el login.
    /// </summary>
    private static readonly TimeSpan DuracionSesion = TimeSpan.FromMinutes(15);

    private readonly IApiClient _api;

    public CuentaController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Admin");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto modelo, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var (ok, datos, error) = await _api.PostAsync<TokenResponseDto>("api/Auth/login", modelo);

        if (!ok || datos is null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Usuario o contraseña incorrectos.");
            return View(modelo);
        }

        // El access token viaja dentro de la cookie cifrada, no en un campo visible
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, datos.NombreUsuario),
            new(ClaimTypes.Role, "Admin"),
            new("AccessToken", datos.AccessToken),
            new("RefreshToken", datos.RefreshToken)
        };

        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            PropiedadesSesion());

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Admin");
    }

    /// <summary>
    /// Cerrar sesión no puede fallar nunca.
    ///
    /// Lo llama tanto el botón del menú (form normal, con antiforgery
    /// token) como sesion-inactividad.js cuando la sesión ya caducó (por
    /// fetch, sin token: para entonces la cookie puede estar tan vieja
    /// como la de sesión misma). Por eso se valida a mano en vez de con
    /// el filtro: si falla, se cierra la sesión igual, porque lo peor que
    /// logra alguien forzando esto es desloguear a quien ya se está yendo.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout([FromServices] IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            // Token vencido o ausente. Se sigue: el objetivo es que salga.
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Estira la sesión otros 60 minutos. La llama sesion-inactividad.js
    /// cuando detecta actividad real del usuario.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Renovar()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Json(new { activa = false });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(User.Identity),
            PropiedadesSesion());

        return Json(new
        {
            activa = true,
            expiraEn = (int)DuracionSesion.TotalSeconds
        });
    }

    /// <summary>Cuántos segundos le quedan a la sesión, sin renovarla.</summary>
    [HttpGet]
    public IActionResult Vigencia()
    {
        var expira = HttpContext.Features
            .Get<IAuthenticateResultFeature>()?
            .AuthenticateResult?.Properties?.ExpiresUtc;

        if (User.Identity?.IsAuthenticated != true)
            return Json(new { activa = false, restan = 0 });

        var restan = expira.HasValue
            ? (int)Math.Max(0, (expira.Value - DateTimeOffset.UtcNow).TotalSeconds)
            : (int)DuracionSesion.TotalSeconds;

        return Json(new { activa = true, restan });
    }

    private static AuthenticationProperties PropiedadesSesion() => new()
    {
        IsPersistent = false,
        ExpiresUtc = DateTimeOffset.UtcNow.Add(DuracionSesion)
    };
}
