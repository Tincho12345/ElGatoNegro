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
        var modelo = new TrabajoFormViewModel
        {
            ApiBaseUrl = _apiBaseUrl
        };

        await CompletarListasAsync(modelo, ct);

        // Desde el panel se pide por fetch para mostrarlo en un modal.
        if (EsPeticionAjax())
            return PartialView("_TrabajoFormModal", modelo);

        return View("TrabajoForm", modelo);
    }

    [HttpGet]
    public async Task<IActionResult> EditarTrabajo(Guid id, CancellationToken ct)
    {
        var trabajo = await _api.GetAsync<TrabajoDto>($"api/Trabajos/{id}", ct);
        if (trabajo is null)
            return NotFound();

        var modelo = new TrabajoFormViewModel
        {
            Id = trabajo.Id,
            Trabajo = new TrabajoCreateDto
            {
                Titulo = trabajo.Titulo,
                Descripcion = trabajo.Descripcion,
                CategoriaId = trabajo.CategoriaId,
                SubcategoriaId = trabajo.SubcategoriaId,
                MarcaId = trabajo.MarcaId,
                Precio = trabajo.Precio,
                PrecioAnterior = trabajo.PrecioAnterior,
                EtiquetaOferta = trabajo.EtiquetaOferta,
                TextoOferta = trabajo.TextoOferta,
                Publicado = trabajo.Publicado,
                Destacado = trabajo.Destacado
            },
            MediosActuales = trabajo.Medios.OrderBy(m => m.Orden).ToList(),
            ApiBaseUrl = _apiBaseUrl
        };

        await CompletarListasAsync(modelo, ct);

        if (EsPeticionAjax())
            return PartialView("_TrabajoFormModal", modelo);

        return View("TrabajoForm", modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarTrabajo(
        TrabajoFormViewModel modelo,
        List<IFormFile>? archivos,
        CancellationToken ct)
    {
        var esAjax = EsPeticionAjax();

        if (!ModelState.IsValid)
        {
            var errores = ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(x => x.ErrorMessage))
                .ToList();

            if (esAjax)
                return Json(new { ok = false, error = string.Join(" ", errores) });

            modelo.ApiBaseUrl = _apiBaseUrl;
            await CompletarListasAsync(modelo, ct);
            return View("TrabajoForm", modelo);
        }

        // Subcategoría y marca son solo de productos: si la categoría es otra,
        // se limpian para que no queden colgadas de un corte o un brushing.
        if (modelo.Trabajo.CategoriaId != await ObtenerCategoriaProductosIdAsync(ct))
        {
            modelo.Trabajo.SubcategoriaId = null;
            modelo.Trabajo.MarcaId = null;
        }

        Guid trabajoId;

        if (modelo.Id is null)
        {
            var (ok, creado, error) = await _api.PostAsync<TrabajoDto>("api/Trabajos", modelo.Trabajo, ct);

            if (!ok || creado is null)
            {
                var mensaje = error ?? "No se pudo crear el trabajo.";

                if (esAjax)
                    return Json(new { ok = false, error = mensaje });

                ModelState.AddModelError(string.Empty, mensaje);
                modelo.ApiBaseUrl = _apiBaseUrl;
                await CompletarListasAsync(modelo, ct);
                return View("TrabajoForm", modelo);
            }

            trabajoId = creado.Id;
        }
        else
        {
            var (ok, error) = await _api.PutAsync($"api/Trabajos/{modelo.Id}", modelo.Trabajo, ct);

            if (!ok)
            {
                var mensaje = error ?? "No se pudo guardar.";

                if (esAjax)
                    return Json(new { ok = false, error = mensaje });

                ModelState.AddModelError(string.Empty, mensaje);
                modelo.ApiBaseUrl = _apiBaseUrl;
                await CompletarListasAsync(modelo, ct);
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
                var mensaje = $"El trabajo se guardó, pero los archivos fallaron: {errorArchivos}";

                if (esAjax)
                    return Json(new { ok = true, id = trabajoId, aviso = mensaje });

                TempData["Error"] = mensaje;
                return RedirectToAction(nameof(EditarTrabajo), new { id = trabajoId });
            }
        }

        if (esAjax)
            return Json(new { ok = true, id = trabajoId });

        TempData["Exito"] = "Trabajo guardado correctamente.";
        return RedirectToAction(nameof(EditarTrabajo), new { id = trabajoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarTrabajo(Guid id, CancellationToken ct)
    {
        var (ok, error) = await _api.DeleteAsync($"api/Trabajos/{id}", ct);

        // Desde el panel se llama por fetch y se espera JSON: un redirect
        // haría que el navegador traiga el HTML y falle al parsearlo.
        if (EsPeticionAjax())
        {
            if (!ok)
                return Json(new { ok = false, error = error ?? "No se pudo eliminar." });

            var trabajos = await _api.GetAsync<List<TrabajoDto>>("api/Trabajos/admin", ct) ?? new();

            return Json(new
            {
                ok = true,
                total = trabajos.Count,
                publicados = trabajos.Count(t => t.Publicado)
            });
        }

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

    // ---------- Subcategorías ----------

    public async Task<IActionResult> Subcategorias(CancellationToken ct)
    {
        var lista = await _api.GetAsync<List<SubcategoriaDto>>("api/Subcategorias/admin", ct) ?? new();
        return View(lista);
    }

    [HttpGet]
    public IActionResult NuevaSubcategoria()
    {
        return View("SubcategoriaForm", new SubcategoriaFormViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> EditarSubcategoria(Guid id, CancellationToken ct)
    {
        var subcategoria = await _api.GetAsync<SubcategoriaDto>($"api/Subcategorias/{id}", ct);
        if (subcategoria is null)
            return NotFound();

        return View("SubcategoriaForm", new SubcategoriaFormViewModel
        {
            Id = subcategoria.Id,
            Subcategoria = new SubcategoriaCreateDto
            {
                Nombre = subcategoria.Nombre,
                Descripcion = subcategoria.Descripcion,
                Icono = subcategoria.Icono,
                Orden = subcategoria.Orden,
                Activa = subcategoria.Activa
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarSubcategoria(SubcategoriaFormViewModel modelo, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View("SubcategoriaForm", modelo);

        string? error;

        if (modelo.Id is null)
        {
            (var ok, _, error) = await _api.PostAsync<SubcategoriaDto>("api/Subcategorias", modelo.Subcategoria, ct);
            if (ok)
            {
                TempData["Exito"] = "Subcategoría creada.";
                return RedirectToAction(nameof(Subcategorias));
            }
        }
        else
        {
            (var ok, error) = await _api.PutAsync($"api/Subcategorias/{modelo.Id}", modelo.Subcategoria, ct);
            if (ok)
            {
                TempData["Exito"] = "Subcategoría actualizada.";
                return RedirectToAction(nameof(Subcategorias));
            }
        }

        ModelState.AddModelError(string.Empty, error ?? "No se pudo guardar la subcategoría.");
        return View("SubcategoriaForm", modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarSubcategoria(Guid id, CancellationToken ct)
    {
        var (ok, error) = await _api.DeleteAsync($"api/Subcategorias/{id}", ct);

        if (ok)
            TempData["Exito"] = "Subcategoría eliminada.";
        else
            TempData["Error"] = error ?? "No se pudo eliminar.";

        return RedirectToAction(nameof(Subcategorias));
    }

    // ---------- Marcas ----------

    public async Task<IActionResult> Marcas(CancellationToken ct)
    {
        var lista = await _api.GetAsync<List<MarcaDto>>("api/Marcas/admin", ct) ?? new();
        return View(lista);
    }

    [HttpGet]
    public IActionResult NuevaMarca()
    {
        return View("MarcaForm", new MarcaFormViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> EditarMarca(Guid id, CancellationToken ct)
    {
        var marca = await _api.GetAsync<MarcaDto>($"api/Marcas/{id}", ct);
        if (marca is null)
            return NotFound();

        return View("MarcaForm", new MarcaFormViewModel
        {
            Id = marca.Id,
            Marca = new MarcaCreateDto
            {
                Nombre = marca.Nombre,
                Orden = marca.Orden,
                Activa = marca.Activa
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarMarca(MarcaFormViewModel modelo, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View("MarcaForm", modelo);

        string? error;

        if (modelo.Id is null)
        {
            (var ok, _, error) = await _api.PostAsync<MarcaDto>("api/Marcas", modelo.Marca, ct);
            if (ok)
            {
                TempData["Exito"] = "Marca creada.";
                return RedirectToAction(nameof(Marcas));
            }
        }
        else
        {
            (var ok, error) = await _api.PutAsync($"api/Marcas/{modelo.Id}", modelo.Marca, ct);
            if (ok)
            {
                TempData["Exito"] = "Marca actualizada.";
                return RedirectToAction(nameof(Marcas));
            }
        }

        ModelState.AddModelError(string.Empty, error ?? "No se pudo guardar la marca.");
        return View("MarcaForm", modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarMarca(Guid id, CancellationToken ct)
    {
        var (ok, error) = await _api.DeleteAsync($"api/Marcas/{id}", ct);

        if (ok)
            TempData["Exito"] = "Marca eliminada.";
        else
            TempData["Error"] = error ?? "No se pudo eliminar.";

        return RedirectToAction(nameof(Marcas));
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

    /// <summary>
    /// Deja el formulario de trabajo con los tres desplegables cargados y con
    /// el Id de Productos, que es lo que la vista mira para mostrar u ocultar
    /// subcategoría y marca.
    /// </summary>
    private async Task CompletarListasAsync(TrabajoFormViewModel modelo, CancellationToken ct)
    {
        var categorias = await _api.GetAsync<List<CategoriaDto>>("api/Categorias/admin", ct) ?? new();

        modelo.Categorias = categorias
            .Where(c => c.Activa)
            .Select(c => new SelectListItem(c.Nombre, c.Id.ToString()))
            .ToList();

        modelo.CategoriaProductosId = BuscarCategoriaProductos(categorias)?.Id;

        var subcategorias = await _api.GetAsync<List<SubcategoriaDto>>("api/Subcategorias/admin", ct) ?? new();

        modelo.Subcategorias = subcategorias
            .Where(s => s.Activa)
            .Select(s => new SelectListItem(s.Nombre, s.Id.ToString()))
            .ToList();

        var marcas = await _api.GetAsync<List<MarcaDto>>("api/Marcas/admin", ct) ?? new();

        modelo.Marcas = marcas
            .Where(m => m.Activa)
            .Select(m => new SelectListItem(m.Nombre, m.Id.ToString()))
            .ToList();
    }

    private async Task<Guid?> ObtenerCategoriaProductosIdAsync(CancellationToken ct)
    {
        var categorias = await _api.GetAsync<List<CategoriaDto>>("api/Categorias/admin", ct) ?? new();
        return BuscarCategoriaProductos(categorias)?.Id;
    }

    /// <summary>
    /// Se identifica por nombre, igual que en la home. Si algún día se renombra
    /// la categoría, hay que tocar las dos partes.
    /// </summary>
    private static CategoriaDto? BuscarCategoriaProductos(List<CategoriaDto> categorias) =>
        categorias.FirstOrDefault(c =>
            string.Equals(c.Nombre?.Trim(), "Productos", StringComparison.OrdinalIgnoreCase));

    /// <summary>La petición vino por fetch desde la vista, no por navegación del navegador.</summary>
    private bool EsPeticionAjax() =>
        Request.Headers["X-Requested-With"] == "XMLHttpRequest";
}
