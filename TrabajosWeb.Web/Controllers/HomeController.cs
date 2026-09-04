using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TrabajosWeb.Shared.DTOs;
using TrabajosWeb.Web.Configuration;
using TrabajosWeb.Web.Models;
using TrabajosWeb.Web.Services;

namespace TrabajosWeb.Web.Controllers;

public class HomeController : Controller
{
    private readonly IApiClient _api;
    private readonly ContactoSettings _contacto;
    private readonly string _apiBaseUrl;

    public HomeController(
        IApiClient api,
        IOptions<ContactoSettings> contacto,
        IOptions<ApiSettings> apiSettings)
    {
        _api = api;
        _contacto = contacto.Value;
        _apiBaseUrl = apiSettings.Value.BaseUrl.TrimEnd('/');
    }

    public async Task<IActionResult> Index(string? categoria, CancellationToken ct)
    {
        var ruta = string.IsNullOrWhiteSpace(categoria)
            ? "api/Trabajos"
            : $"api/Trabajos?categoria={Uri.EscapeDataString(categoria)}";

        var trabajos = await _api.GetAsync<List<TrabajoDto>>(ruta, ct) ?? new();
        var categorias = await _api.GetAsync<List<CategoriaDto>>("api/Categorias", ct) ?? new();

        var modelo = new GaleriaViewModel
        {
            Trabajos = trabajos,
            Categorias = categorias,
            CategoriaActual = categoria,
            Contacto = _contacto,
            ApiBaseUrl = _apiBaseUrl
        };

        // Al filtrar desde la web se pide solo la grilla, para no recargar
        // la página entera. Ver wwwroot/js/galeria.js
        if (EsPeticionAjax())
            return PartialView("_GaleriaGrilla", modelo);

        modelo.Servicios = await _api.GetAsync<List<CategoriaDto>>("api/Categorias/servicios", ct) ?? new();

        return View(modelo);
    }

    public async Task<IActionResult> Detalle(Guid id, CancellationToken ct)
    {
        var trabajo = await _api.GetAsync<TrabajoDto>($"api/Trabajos/{id}", ct);

        if (trabajo is null)
            return NotFound();

        // Otros trabajos de la misma categoría, para seguir mirando.
        var relacionados = new List<TrabajoDto>();

        if (!string.IsNullOrWhiteSpace(trabajo.CategoriaSlug))
        {
            var mismos = await _api.GetAsync<List<TrabajoDto>>(
                $"api/Trabajos?categoria={Uri.EscapeDataString(trabajo.CategoriaSlug)}", ct) ?? new();

            relacionados = mismos
                .Where(t => t.Id != trabajo.Id)
                .Take(3)
                .ToList();
        }

        return View(new GaleriaViewModel
        {
            Trabajos = new List<TrabajoDto> { trabajo },
            Relacionados = relacionados,
            Contacto = _contacto,
            ApiBaseUrl = _apiBaseUrl
        });
    }

    [HttpGet]
    public IActionResult Contacto()
    {
        return View(new ContactoViewModel { Contacto = _contacto });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contacto(ContactoViewModel modelo, CancellationToken ct)
    {
        modelo.Contacto = _contacto;

        if (!ModelState.IsValid)
            return View(modelo);

        var (ok, _, error) = await _api.PostAsync<object>("api/Consultas", modelo.Consulta, ct);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "No se pudo enviar la consulta.");
            return View(modelo);
        }

        TempData["Exito"] = "Recibimos tu mensaje. Te respondemos a la brevedad.";
        return RedirectToAction(nameof(Contacto));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();

    /// <summary>La petición vino por fetch desde la vista, no por navegación del navegador.</summary>
    private bool EsPeticionAjax() =>
        Request.Headers["X-Requested-With"] == "XMLHttpRequest";
}