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
    /// Cantidad total para la campanita/badge: consultas + pedidos sin
    /// leer. push.js pega acá al cargar cualquier vista del panel y cada
    /// 60s, y también cuando el service worker le avisa que llegó un push
    /// nuevo (de cualquiera de los dos tipos: ambos usan el mismo evento
    /// 'push' del lado del navegador).
    /// </summary>
    [HttpGet("no-leidas")]
    public async Task<IActionResult> NoLeidas(CancellationToken ct)
    {
        var consultas = await _api.GetAsync<List<ConsultaDto>>("api/Consultas?soloNoLeidas=true", ct) ?? new();
        var pedidos = await _api.GetAsync<List<PedidoDto>>("api/Pedidos?soloNoLeidos=true", ct) ?? new();

        return Ok(new { cantidad = consultas.Count + pedidos.Count });
    }
}
