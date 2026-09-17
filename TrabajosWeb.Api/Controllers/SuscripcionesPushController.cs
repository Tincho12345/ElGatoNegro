using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrabajosWeb.Api.Configuration;
using TrabajosWeb.Api.Data;
using TrabajosWeb.Api.Entities;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuscripcionesPushController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly VapidSettings _vapid;

    public SuscripcionesPushController(AppDbContext context, IOptions<VapidSettings> vapid)
    {
        _context = context;
        _vapid = vapid.Value;
    }

    /// <summary>
    /// La clave pública no es secreta: el navegador la necesita para armar
    /// la suscripción, así que se entrega sin pedir token.
    /// </summary>
    [HttpGet("clave-publica")]
    [AllowAnonymous]
    public ActionResult<ClavePublicaDto> GetClavePublica()
    {
        if (string.IsNullOrWhiteSpace(_vapid.PublicKey))
            return NotFound(new { mensaje = "Las notificaciones push no están configuradas." });

        // Se envuelve en un DTO a propósito: un string suelto devuelto con
        // Ok() sale como texto plano, no como JSON, y ReadFromJsonAsync
        // del lado del cliente falla al intentar parsearlo.
        return Ok(new ClavePublicaDto { PublicKey = _vapid.PublicKey });
    }

    [HttpPost]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Suscribir(SuscripcionPushCreateDto dto, CancellationToken ct)
    {
        var existente = await _context.SuscripcionesPush
            .FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint, ct);

        if (existente is not null)
        {
            // Mismo endpoint: se actualizan las claves por las dudas, en vez
            // de rechazar el alta.
            existente.P256dh = dto.P256dh;
            existente.Auth = dto.Auth;
            existente.Dispositivo = dto.Dispositivo;
            await _context.SaveChangesAsync(ct);

            // Ok con cuerpo, no NoContent: PostAsync<TRespuesta> del lado
            // del cliente siempre intenta leer JSON de la respuesta, y un
            // 204 sin contenido le hace tirar una excepción al deserializar.
            return Ok(new { ok = true });
        }

        _context.SuscripcionesPush.Add(new SuscripcionPush
        {
            Endpoint = dto.Endpoint,
            P256dh = dto.P256dh,
            Auth = dto.Auth,
            Dispositivo = dto.Dispositivo
        });

        await _context.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    /// <summary>
    /// El endpoint va por query string, no por body: así funciona con
    /// IApiClient.DeleteAsync, que en este proyecto no manda cuerpo en los
    /// DELETE.
    /// </summary>
    [HttpDelete]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Desuscribir([FromQuery] string endpoint, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return BadRequest();

        var existente = await _context.SuscripcionesPush
            .FirstOrDefaultAsync(s => s.Endpoint == endpoint, ct);

        if (existente is not null)
        {
            _context.SuscripcionesPush.Remove(existente);
            await _context.SaveChangesAsync(ct);
        }

        return NoContent();
    }
}
