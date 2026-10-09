using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrabajosWeb.Shared.DTOs;
using TrabajosWeb.Web.Services;

namespace TrabajosWeb.Web.Controllers;

/// <summary>
/// Lo que Mercado Pago necesita alcanzar desde internet: el aviso de pago
/// (webhook) y la página a la que vuelve la clienta. La API no está
/// publicada, así que ambos pasan por el sitio y se reenvían a ella.
/// </summary>
[AllowAnonymous]
[Route("pagos")]
public class PagosController : Controller
{
    private readonly IApiClient _api;
    private readonly ILogger<PagosController> _logger;

    public PagosController(IApiClient api, ILogger<PagosController> logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <summary>
    /// Mercado Pago avisa con el id del pago, nunca con el resultado: el
    /// estado real se pide a su API desde acá. Si algo falla se responde un
    /// error para que reintente el aviso más tarde.
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var paymentId = await LeerIdDePagoAsync(ct);

        // Avisos de otros tipos (órdenes, planes, etc.): se aceptan y se ignoran.
        if (paymentId is null)
            return Ok();

        var (ok, _, error) = await _api.PostAsync<EstadoPedidoDto>(
            "api/Pagos/sincronizar", new SincronizarPagoDto { PaymentId = paymentId }, ct);

        if (!ok)
        {
            _logger.LogWarning("No se pudo procesar el aviso del pago {PaymentId}: {Error}", paymentId, error);
            return StatusCode(StatusCodes.Status502BadGateway);
        }

        return Ok();
    }

    [HttpGet("resultado")]
    public async Task<IActionResult> Resultado(
        Guid pedido,
        [FromQuery(Name = "payment_id")] string? paymentId,
        CancellationToken ct)
    {
        EstadoPedidoDto? estado = null;

        // Confirmar el pago acá hace que la clienta vea el resultado real
        // aunque el aviso del webhook todavía no haya llegado.
        if (!string.IsNullOrWhiteSpace(paymentId))
        {
            var (ok, datos, _) = await _api.PostAsync<EstadoPedidoDto>(
                "api/Pagos/sincronizar", new SincronizarPagoDto { PaymentId = paymentId }, ct);

            if (ok && datos is not null && datos.PedidoId != Guid.Empty)
                estado = datos;
        }

        estado ??= await _api.GetAsync<EstadoPedidoDto>($"api/Pagos/estado/{pedido}", ct);

        ViewBag.HuboIntentoDePago = !string.IsNullOrWhiteSpace(paymentId);

        return View(estado);
    }

    /// <summary>
    /// El aviso llega de dos formas: JSON en el cuerpo
    /// ({"type":"payment","data":{"id":"123"}}) o, en el formato viejo (IPN),
    /// por la URL (?topic=payment&amp;id=123).
    /// </summary>
    private async Task<string?> LeerIdDePagoAsync(CancellationToken ct)
    {
        var tipo = Request.Query["type"].ToString();
        if (string.IsNullOrEmpty(tipo)) tipo = Request.Query["topic"].ToString();

        var id = Request.Query["data.id"].ToString();
        if (string.IsNullOrEmpty(id)) id = Request.Query["id"].ToString();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(tipo))
        {
            try
            {
                using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
                var raiz = doc.RootElement;

                if (string.IsNullOrEmpty(tipo) && raiz.TryGetProperty("type", out var t))
                    tipo = t.GetString() ?? string.Empty;

                if (string.IsNullOrEmpty(id)
                    && raiz.TryGetProperty("data", out var datos)
                    && datos.TryGetProperty("id", out var i))
                    id = i.ToString();
            }
            catch (JsonException)
            {
                // Cuerpo vacío o que no es JSON: se usa lo que haya venido en la URL.
            }
        }

        return tipo == "payment" && !string.IsNullOrEmpty(id) ? id : null;
    }
}
