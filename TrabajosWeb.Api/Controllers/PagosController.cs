using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrabajosWeb.Api.Data;
using TrabajosWeb.Api.Services;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class PagosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMercadoPagoService _mercadoPago;
    private readonly IPushNotificationService _push;
    private readonly ICobroOnlineService _cobroOnline;
    private readonly ILogger<PagosController> _logger;

    public PagosController(
        AppDbContext context,
        IMercadoPagoService mercadoPago,
        IPushNotificationService push,
        ICobroOnlineService cobroOnline,
        ILogger<PagosController> logger)
    {
        _context = context;
        _mercadoPago = mercadoPago;
        _push = push;
        _cobroOnline = cobroOnline;
        _logger = logger;
    }

    /// <summary>Le dice al sitio qué formas de pago ofrecer en el carrito.</summary>
    [HttpGet("disponible")]
    public async Task<IActionResult> Disponible(CancellationToken ct) =>
        Ok(new OpcionesPagoDto { MercadoPago = await _cobroOnline.MercadoPagoHabilitadoAsync(ct) });

    /// <summary>
    /// Crea el link de pago de un pedido. El monto sale de las líneas ya
    /// guardadas del pedido (precios recalculados en el servidor), nunca de
    /// lo que mande el navegador.
    /// </summary>
    [HttpPost("{pedidoId:guid}/preferencia")]
    public async Task<IActionResult> CrearPreferencia(
        Guid pedidoId, PreferenciaPagoRequestDto dto, CancellationToken ct)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == pedidoId, ct);

        if (pedido is null)
            return NotFound(new { mensaje = "No encontramos el pedido." });

        if (pedido.MetodoPago != MetodoPago.MercadoPago)
            return BadRequest(new { mensaje = "Este pedido no es para pagar online." });

        if (pedido.EstadoPago == EstadoPago.Aprobado)
            return BadRequest(new { mensaje = "Este pedido ya está pago." });

        var url = await _mercadoPago.CrearPreferenciaAsync(pedido, dto.UrlRetorno, dto.UrlNotificacion, ct);

        if (url is null)
            return StatusCode(StatusCodes.Status502BadGateway,
                new { mensaje = "No pudimos iniciar el pago con Mercado Pago. Probá de nuevo en un momento." });

        return Ok(new PreferenciaPagoDto { UrlPago = url });
    }

    /// <summary>
    /// Trae el pago desde Mercado Pago y actualiza el pedido. Lo usan tanto el
    /// aviso (webhook) como la página de retorno, así que es idempotente: si
    /// el pago ya estaba registrado no cambia nada ni vuelve a notificar.
    /// </summary>
    [HttpPost("sincronizar")]
    public async Task<IActionResult> Sincronizar(SincronizarPagoDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.PaymentId) || !dto.PaymentId.All(char.IsAsciiDigit))
            return BadRequest(new { mensaje = "Id de pago inválido." });

        var pago = await _mercadoPago.ObtenerPagoAsync(dto.PaymentId, ct);

        if (pago is null)
            return StatusCode(StatusCodes.Status502BadGateway,
                new { mensaje = "No pudimos consultar el pago." });

        // Un pago ajeno a los pedidos del sitio no es un error: se responde
        // 200 vacío para que Mercado Pago no siga reintentando el aviso.
        if (!Guid.TryParse(pago.ReferenciaExterna, out var pedidoId))
            return Ok(new EstadoPedidoDto());

        var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId, ct);

        if (pedido is null)
            return Ok(new EstadoPedidoDto());

        var nuevoEstado = TraducirEstado(pago.Estado);

        // Un pago aprobado solo puede pasar a reembolsado; cualquier otro
        // aviso tardío (por ejemplo uno viejo "pendiente") no lo pisa.
        var puedeCambiar = pedido.EstadoPago != EstadoPago.Aprobado || nuevoEstado == EstadoPago.Reembolsado;

        // Si el monto cobrado no cubre el total, no se da por pago.
        if (nuevoEstado == EstadoPago.Aprobado && pago.Monto < pedido.Total)
        {
            _logger.LogWarning(
                "Pedido {Numero}: el pago {PaymentId} es por {Monto} y el total es {Total}. No se marca como pago.",
                pedido.Numero, pago.Id, pago.Monto, pedido.Total);
            puedeCambiar = false;
        }

        if (puedeCambiar && pedido.EstadoPago != nuevoEstado)
        {
            var recienAprobado = nuevoEstado == EstadoPago.Aprobado;

            pedido.EstadoPago = nuevoEstado;
            pedido.MercadoPagoPaymentId = pago.Id;

            if (recienAprobado)
            {
                pedido.PagadoEl = DateTime.UtcNow;
                // Los pedidos con pago online nacen como leídos para que un
                // pago abandonado no ensucie el aviso: recién al cobrarse
                // aparecen como novedad en el panel.
                pedido.Leido = false;
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Pedido {Numero}: pago {PaymentId} pasó a {Estado}",
                pedido.Numero, pago.Id, nuevoEstado);

            if (recienAprobado)
            {
                await _push.NotificarATodosAsync(
                    "Pedido pago",
                    $"{pedido.Nombre}: pedido N° {pedido.Numero} — {pedido.Total.ToString("C0")} (Mercado Pago)",
                    "/Admin/Pedidos",
                    ct: ct);
            }
        }

        return Ok(new EstadoPedidoDto
        {
            PedidoId = pedido.Id,
            Numero = pedido.Numero,
            EstadoPago = pedido.EstadoPago
        });
    }

    [HttpGet("estado/{pedidoId:guid}")]
    public async Task<IActionResult> Estado(Guid pedidoId, CancellationToken ct)
    {
        var pedido = await _context.Pedidos
            .AsNoTracking()
            .Where(p => p.Id == pedidoId)
            .Select(p => new EstadoPedidoDto
            {
                PedidoId = p.Id,
                Numero = p.Numero,
                EstadoPago = p.EstadoPago
            })
            .FirstOrDefaultAsync(ct);

        return pedido is null
            ? NotFound(new { mensaje = "No encontramos el pedido." })
            : Ok(pedido);
    }

    private static EstadoPago TraducirEstado(string estadoMercadoPago) => estadoMercadoPago switch
    {
        "approved" => EstadoPago.Aprobado,
        "rejected" or "cancelled" => EstadoPago.Rechazado,
        "refunded" or "charged_back" => EstadoPago.Reembolsado,
        _ => EstadoPago.Pendiente // pending, in_process, authorized, in_mediation
    };
}
