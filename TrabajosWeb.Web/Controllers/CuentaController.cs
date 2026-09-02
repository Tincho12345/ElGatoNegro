using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TrabajosWeb.Shared.DTOs;
using TrabajosWeb.Web.Services;

namespace TrabajosWeb.Web.Controllers;

public class CuentaController : Controller
{
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
            new ClaimsPrincipal(identidad));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Admin");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}