using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using TrabajosWeb.Shared.DTOs;
using TrabajosWeb.Web.Configuration;
using TrabajosWeb.Web.Models;
using TrabajosWeb.Web.Services;

namespace TrabajosWeb.Web.Controllers;

[Authorize(Policy = "SoloAdmin")]
public class AdminController : Controller
{
    private readonly IApiClient _api;
    private readonly IAjustesService _ajustes;
    private readonly string _apiBaseUrl;

    public AdminController(
        IApiClient api,
        IAjustesService ajustes,
        IOptions<ApiSettings> apiSettings)
    {
        _api = api;
        _ajustes = ajustes;
        _apiBaseUrl = apiSettings.Value.BaseUrl.TrimEnd('/');
    }

    // ---------- Panel ----------

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var trabajos = await _api.GetAsync<List<TrabajoDto>>("api/Trabajos/admin", ct) ?? new();
        var categorias = await _api.GetAsync<List<CategoriaDto>>("api/Categorias/admin", ct) ?? new();
        var consultas = await _api.GetAsync<List<ConsultaDto>>("api/Consultas?soloNoLeidas=true", ct) ?? new();

        return View(new PanelViewModel
        {
            Trabajos = trabajos,
            Categorias = categorias,
            ConsultasSinLeer = consultas.Count,
            ApiBaseUrl = _apiBaseUrl
        });
    }

    // ---------- Trabajos ----------

    [HttpGet]
    public async Task<IActionResult> NuevoTrabajo(CancellationToken ct)
    {
        return View("TrabajoForm", new TrabajoFormViewModel
        {
            Categorias = await CargarCategoriasAsync(ct),
            ApiBaseUrl = _apiBaseUrl
        });
    }

    [HttpGet]
    public async Task<IActionResult> EditarTrabajo(Guid id, CancellationToken ct)
    {
        var trabajo = await _api.GetAsync<TrabajoDto>($"api/Trabajos/{id}", ct);
        if (trabajo is null)
            return NotFound();

        return View("TrabajoForm", new TrabajoFormViewModel
        {
            Id = trabajo.Id,
            Trabajo = new TrabajoCreateDto
            {
                Titulo = trabajo.Titulo,
                Descripcion = trabajo.Descripcion,
                CategoriaId = trabajo.CategoriaId,
                Precio = trabajo.Precio,
                PrecioAnterior = trabajo.PrecioAnterior,
                EtiquetaOferta = trabajo.EtiquetaOferta,
                TextoOferta = trabajo.TextoOferta,
                Publicado = trabajo.Publicado,
                Destacado = trabajo.Destacado
            },
            MediosActuales = trabajo.Medios.OrderBy(m => m.Orden).ToList(),
            Categorias = await CargarCategoriasAsync(ct),
            ApiBaseUrl = _apiBaseUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarTrabajo(
        TrabajoFormViewModel modelo,
        List<IFormFile>? archivos,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            modelo.Categorias = await CargarCategoriasAsync(ct);
            modelo.ApiBaseUrl = _apiBaseUrl;
            return View("TrabajoForm", modelo);
        }

        Guid trabajoId;

        if (modelo.Id is null)
        {
            var (ok, creado, error) = await _api.PostAsync<TrabajoDto>("api/Trabajos", modelo.Trabajo, ct);

            if (!ok || creado is null)
            {
                ModelState.AddModelError(string.Empty, error ?? "No se pudo crear el trabajo.");
                modelo.Categorias = await CargarCategoriasAsync(ct);
                modelo.ApiBaseUrl = _apiBaseUrl;
                return View("TrabajoForm", modelo);
            }

            trabajoId = creado.Id;
        }
        else
        {
            var (ok, error) = await _api.PutAsync($"api/Trabajos/{modelo.Id}", modelo.Trabajo, ct);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "No se pudo guardar.");
                modelo.Categorias = await CargarCategoriasAsync(ct);
                modelo.ApiBaseUrl = _apiBaseUrl;
                return View("TrabajoForm", modelo);
            }

            trabajoId = modelo.Id.Value;
        }

        // Los archivos se suben aparte, después de tener el Id
        if (archivos is not null && archivos.Count > 0)
        {
            var (okArchivos, errorArchivos) = await _api.PostArchivosAsync(
                $"api/trabajos/{trabajoId}/media", archivos, ct);

            if (!okArchivos)
            {
                TempData["Error"] = $"El trabajo se guardó, pero los archivos fallaron: {errorArchivos}";
                return RedirectToAction(nameof(EditarTrabajo), new { id = trabajoId });
            }
        }

        TempData["Exito"] = "Trabajo guardado correctamente.";
        return RedirectToAction(nameof(EditarTrabajo), new { id = trabajoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarTrabajo(Guid id, CancellationToken ct)
    {
        var (ok, error) = await _api.DeleteAsync($"api/Trabajos/{id}", ct);

        if (ok)
            TempData["Exito"] = "Trabajo eliminado.";
        else
            TempData["Error"] = error ?? "No se pudo eliminar.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarMedia(Guid trabajoId, Guid mediaId, CancellationToken ct)
    {
        var (ok, error) = await _api.DeleteAsync($"api/trabajos/{trabajoId}/media/{mediaId}", ct);

        // Desde la pantalla de edición se llama por fetch: respondemos JSON
        // para no recargar. Sin JavaScript, sigue funcionando la redirección.
        if (EsPeticionAjax())
            return Json(new { ok, error = ok ? null : (error ?? "No se pudo eliminar el archivo.") });

        if (!ok)
            TempData["Error"] = error ?? "No se pudo eliminar el archivo.";

        return RedirectToAction(nameof(EditarTrabajo), new { id = trabajoId });
    }

    // ---------- Categorías ----------

    public async Task<IActionResult> Categorias(CancellationToken ct)
    {
        var categorias = await _api.GetAsync<List<CategoriaDto>>("api/Categorias/admin", ct) ?? new();
        return View(categorias);
    }

    [HttpGet]
    public IActionResult NuevaCategoria()
    {
        return View("CategoriaForm", new CategoriaFormViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> EditarCategoria(Guid id, CancellationToken ct)
    {
        var categoria = await _api.GetAsync<CategoriaDto>($"api/Categorias/{id}", ct);
        if (categoria is null)
            return NotFound();

        return View("CategoriaForm", new CategoriaFormViewModel
        {
            Id = categoria.Id,
            Categoria = new CategoriaCreateDto
            {
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion,
                Icono = categoria.Icono,
                TextoServicio = categoria.TextoServicio,
                MostrarEnServicios = categoria.MostrarEnServicios,
                Orden = categoria.Orden,
                Activa = categoria.Activa
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarCategoria(CategoriaFormViewModel modelo, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View("CategoriaForm", modelo);

        string? error;

        if (modelo.Id is null)
        {
            (var ok, _, error) = await _api.PostAsync<CategoriaDto>("api/Categorias", modelo.Categoria, ct);
            if (ok)
            {
                TempData["Exito"] = "Categoría creada.";
                return RedirectToAction(nameof(Categorias));
            }
        }
        else
        {
            (var ok, error) = await _api.PutAsync($"api/Categorias/{modelo.Id}", modelo.Categoria, ct);
            if (ok)
            {
                TempData["Exito"] = "Categoría actualizada.";
                return RedirectToAction(nameof(Categorias));
            }
        }

        ModelState.AddModelError(string.Empty, error ?? "No se pudo guardar la categoría.");
        return View("CategoriaForm", modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCategoria(Guid id, CancellationToken ct)
    {
        var (ok, error) = await _api.DeleteAsync($"api/Categorias/{id}", ct);

        if (ok)
            TempData["Exito"] = "Categoría eliminada.";
        else
            TempData["Error"] = error ?? "No se pudo eliminar.";

        return RedirectToAction(nameof(Categorias));
    }

    // ---------- Datos del sitio ----------

    [HttpGet]
    public async Task<IActionResult> Ajustes(CancellationToken ct)
    {
        var ajustes = await _api.GetAsync<AjustesSitioDto>("api/Ajustes", ct);

        if (ajustes is null)
        {
            TempData["Error"] = "No se pudieron leer los datos del sitio.";
            return RedirectToAction(nameof(Index));
        }

        return View("AjustesForm", new AjustesFormViewModel
        {
            Ajustes = new AjustesSitioUpdateDto
            {
                NombreSitio = ajustes.NombreSitio,
                Saludo = ajustes.Saludo,
                TextoBienvenida = ajustes.TextoBienvenida,
                WhatsAppNumero = ajustes.WhatsAppNumero,
                MensajeWhatsApp = ajustes.MensajeWhatsApp,
                Email = ajustes.Email,
                Telefono = ajustes.Telefono,
                Direccion = ajustes.Direccion,
                Horarios = ajustes.Horarios,
                Facebook = ajustes.Facebook,
                Instagram = ajustes.Instagram,
                TikTok = ajustes.TikTok
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarAjustes(AjustesFormViewModel modelo, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View("AjustesForm", modelo);

        var (ok, error) = await _api.PutAsync("api/Ajustes", modelo.Ajustes, ct);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "No se pudieron guardar los datos.");
            return View("AjustesForm", modelo);
        }

        // El layout los tiene cacheados: hay que soltarlos para que se vean ya.
        _ajustes.Invalidar();

        TempData["Exito"] = "Datos del sitio actualizados.";
        return RedirectToAction(nameof(Ajustes));
    }

    // ---------- Consultas ----------

    public async Task<IActionResult> Consultas(bool soloNoLeidas = false, CancellationToken ct = default)
    {
        var ruta = soloNoLeidas ? "api/Consultas?soloNoLeidas=true" : "api/Consultas";
        var consultas = await _api.GetAsync<List<ConsultaDto>>(ruta, ct) ?? new();

        return View(new ConsultasViewModel
        {
            Consultas = consultas,
            SoloNoLeidas = soloNoLeidas
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLeida(Guid id, CancellationToken ct)
    {
        await _api.PatchAsync($"api/Consultas/{id}/leida", ct);
        return RedirectToAction(nameof(Consultas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConsulta(Guid id, CancellationToken ct)
    {
        var (ok, error) = await _api.DeleteAsync($"api/Consultas/{id}", ct);

        if (!ok)
            TempData["Error"] = error ?? "No se pudo eliminar.";

        return RedirectToAction(nameof(Consultas));
    }

    // ---------- Auxiliares ----------

    private async Task<List<SelectListItem>> CargarCategoriasAsync(CancellationToken ct)
    {
        var categorias = await _api.GetAsync<List<CategoriaDto>>("api/Categorias/admin", ct) ?? new();

        return categorias
            .Where(c => c.Activa)
            .Select(c => new SelectListItem(c.Nombre, c.Id.ToString()))
            .ToList();
    }

    /// <summary>La petición vino por fetch desde la vista, no por navegación del navegador.</summary>
    private bool EsPeticionAjax() =>
        Request.Headers["X-Requested-With"] == "XMLHttpRequest";
}