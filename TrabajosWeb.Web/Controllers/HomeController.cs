using Microsoft.AspNetCore.DataProtection;
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
    private readonly IDataProtector _protector;
    private readonly ILogger<HomeController> _logger;

    /// <summary>Menos que esto entre abrir y enviar el formulario es un bot.</summary>
    private static readonly TimeSpan TiempoMinimo = TimeSpan.FromSeconds(3);

    /// <summary>Pasado este rato el sello vence y hay que recargar.</summary>
    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromHours(3);

    public HomeController(
        IApiClient api,
        IOptions<ContactoSettings> contacto,
        IOptions<ApiSettings> apiSettings,
        IDataProtectionProvider protectorProvider,
        ILogger<HomeController> logger)
    {
        _api = api;
        _contacto = contacto.Value;
        _apiBaseUrl = apiSettings.Value.BaseUrl.TrimEnd('/');
        _protector = protectorProvider.CreateProtector("TrabajosWeb.FormularioConsulta");
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? categoria, CancellationToken ct)
    {
        // Traemos todos los trabajos siempre: el filtro por categoría lo
        // resuelve Isotope en el navegador, sin volver al servidor.
        // El parámetro sigue existiendo para que el enlace directo a una
        // categoría abra la galería ya filtrada.
        var trabajos = await _api.GetAsync<List<TrabajoDto>>("api/Trabajos", ct) ?? new();
        var categorias = await _api.GetAsync<List<CategoriaDto>>("api/Categorias", ct) ?? new();
        var servicios = await _api.GetAsync<List<CategoriaDto>>("api/Categorias/servicios", ct) ?? new();

        // Si el slug no existe, mostramos todo en lugar de una galería vacía.
        if (!string.IsNullOrWhiteSpace(categoria)
            && !categorias.Any(c => c.Slug == categoria))
        {
            categoria = null;
        }

        return View(new GaleriaViewModel
        {
            Trabajos = trabajos,
            Categorias = categorias,
            Servicios = servicios,
            CategoriaActual = categoria,
            Contacto = _contacto,
            ApiBaseUrl = _apiBaseUrl
        });
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
    public async Task<IActionResult> Contacto(Guid? trabajo, CancellationToken ct)
    {
        var modelo = new ContactoViewModel
        {
            Contacto = _contacto,
            ApiBaseUrl = _apiBaseUrl,
            Sello = NuevoSello()
        };

        // Se llega desde el detalle: el mensaje viene escrito para que la
        // persona solo agregue lo suyo.
        if (trabajo.HasValue)
        {
            var elegido = await _api.GetAsync<TrabajoDto>($"api/Trabajos/{trabajo}", ct);

            if (elegido is not null)
            {
                modelo.Trabajo = elegido;
                modelo.Consulta.Mensaje = ArmarMensaje(elegido);
            }
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contacto(ContactoViewModel modelo, CancellationToken ct)
    {
        modelo.Contacto = _contacto;
        modelo.ApiBaseUrl = _apiBaseUrl;

        // Al bot se le responde como si todo hubiera salido bien: si se entera
        // de que lo detectamos, prueba con otra cosa.
        if (EsSpam(modelo))
        {
            TempData["Exito"] = "Recibimos tu mensaje. Te respondemos a la brevedad.";
            return RedirectToAction(nameof(Contacto));
        }

        if (!ModelState.IsValid)
        {
            modelo.Sello = NuevoSello();
            return View(modelo);
        }

        var (ok, _, error) = await _api.PostAsync<object>("api/Consultas", modelo.Consulta, ct);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "No se pudo enviar la consulta.");
            modelo.Sello = NuevoSello();
            return View(modelo);
        }

        TempData["Exito"] = "Recibimos tu mensaje. Te respondemos a la brevedad.";
        return RedirectToAction(nameof(Contacto));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();

    // ---------- Anti spam ----------

    private string NuevoSello() =>
        _protector.Protect(DateTime.UtcNow.Ticks.ToString());

    private bool EsSpam(ContactoViewModel modelo)
    {
        // El campo trampa no se ve en pantalla: si viene completo, lo llenó
        // algo que leyó el HTML.
        if (!string.IsNullOrWhiteSpace(modelo.Web))
        {
            _logger.LogInformation("Consulta descartada: cayó en el campo trampa.");
            return true;
        }

        if (string.IsNullOrWhiteSpace(modelo.Sello))
        {
            _logger.LogInformation("Consulta descartada: llegó sin sello.");
            return true;
        }

        long ticks;

        try
        {
            ticks = long.Parse(_protector.Unprotect(modelo.Sello));
        }
        catch
        {
            // Firma inválida: el sello se fabricó afuera.
            _logger.LogInformation("Consulta descartada: sello inválido.");
            return true;
        }

        var transcurrido = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);

        if (transcurrido < TiempoMinimo)
        {
            _logger.LogInformation("Consulta descartada: enviada en {Segundos}s.",
                transcurrido.TotalSeconds);
            return true;
        }

        if (transcurrido > TiempoMaximo)
        {
            _logger.LogInformation("Consulta descartada: el formulario estuvo abierto demasiado tiempo.");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Texto inicial de la consulta. Deja una línea en blanco al final para
    /// que se note dónde escribir.
    /// </summary>
    private static string ArmarMensaje(TrabajoDto trabajo)
    {
        var lineas = new List<string> { $"Hola, quiero consultar por: {trabajo.Titulo}" };

        if (!string.IsNullOrWhiteSpace(trabajo.MarcaNombre))
            lineas.Add($"Marca: {trabajo.MarcaNombre}");

        if (trabajo.Precio.HasValue)
            lineas.Add($"Precio publicado: {trabajo.Precio.Value:C0}");

        lineas.Add(string.Empty);

        return string.Join(Environment.NewLine, lineas);
    }
}
