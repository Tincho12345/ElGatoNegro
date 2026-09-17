using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrabajosWeb.Shared.DTOs;
using TrabajosWeb.Web.Services;

namespace TrabajosWeb.Web.Controllers;

/// <summary>
/// Puente entre el navegador y la API para las notificaciones push. El
/// navegador no tiene a mano el token de la API —vive del lado del
/// servidor, en la cookie de sesión de acá— así que estas acciones se lo
/// agregan por él, igual que hace IApiClient con cualquier otra llamada
/// del panel.
/// </summary>
[Authorize(Policy = "SoloAdmin")]
[Route("Notificaciones")]
public class NotificacionesController : Controller
{
    private readonly IApiClient _api;

    public NotificacionesController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet("clave-publica")]
    public async Task<IActionResult> ClavePublica(CancellationToken ct)
    {
        var resultado = await _api.GetAsync<ClavePublicaDto>("api/SuscripcionesPush/clave-publica", ct);

        if (resultado is null || string.IsNullOrWhiteSpace(resultado.PublicKey))
            return NotFound();

        return Ok(resultado);
    }

    [HttpPost("suscribir")]
    public async Task<IActionResult> Suscribir([FromBody] SuscripcionPushCreateDto dto, CancellationToken ct)
    {
        var (ok, _, error) = await _api.PostAsync<object>("api/SuscripcionesPush", dto, ct);
        return ok ? Ok() : BadRequest(new { mensaje = error });
    }

    [HttpPost("desuscribir")]
    public async Task<IActionResult> Desuscribir([FromBody] string endpoint, CancellationToken ct)
    {
        var (ok, error) = await _api.DeleteAsync(
            $"api/SuscripcionesPush?endpoint={Uri.EscapeDataString(endpoint)}", ct);

        return ok ? Ok() : BadRequest(new { mensaje = error });
    }

    /// <summary>
    /// Cantidad de consultas sin leer, para el contador de la campanita.
    /// Reutiliza el mismo listado que ya usa el panel (GetTodas con
    /// soloNoLeidas=true) en vez de sumar un endpoint nuevo del lado de
    /// la API.
    /// </summary>
    [HttpGet("no-leidas")]
    public async Task<IActionResult> NoLeidas(CancellationToken ct)
    {
        var lista = await _api.GetAsync<List<ConsultaDto>>("api/Consultas?soloNoLeidas=true", ct);
        return Ok(new { cantidad = lista?.Count ?? 0 });
    }
}
