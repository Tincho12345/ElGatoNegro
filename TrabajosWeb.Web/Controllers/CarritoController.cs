using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrabajosWeb.Shared.DTOs;
using TrabajosWeb.Web.Services;

namespace TrabajosWeb.Web.Controllers;

/// <summary>
/// Puente entre el carrito del sitio y la API. El carrito vive en el
/// localStorage del navegador (sin cuenta, sin sesión), así que esta acción
/// es pública: cualquier visitante puede confirmar un pedido.
/// </summary>
[AllowAnonymous]
[Route("pedidos")]
public class CarritoController : Controller
{
    private readonly IApiClient _api;

    public CarritoController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet("opciones")]
    public async Task<IActionResult> Opciones(CancellationToken ct)
    {
        var opciones = await _api.GetAsync<OpcionesPagoDto>("api/Pagos/disponible", ct);

        return Ok(new { mercadoPago = opciones?.MercadoPago == true });
    }

    [HttpPost("confirmar")]
    public async Task<IActionResult> Confirmar([FromBody] PedidoCreateDto dto, CancellationToken ct)
    {
        var (ok, creado, error) = await _api.PostAsync<PedidoDto>("api/Pedidos", dto, ct);

        if (!ok || creado is null)
            return Ok(new { ok = false, error = error ?? "No se pudo registrar el pedido." });

        string? urlPago = null;

        if (creado.MetodoPago == MetodoPago.MercadoPago)
        {
            // Las direcciones las arma el servidor: el navegador no decide a
            // dónde vuelve ni dónde avisa Mercado Pago.
            var sitio = $"{Request.Scheme}://{Request.Host}";

            var pedirLink = new PreferenciaPagoRequestDto
            {
                UrlRetorno = $"{sitio}/pagos/resultado",
                // Mercado Pago no puede avisar a localhost: solo en https.
                UrlNotificacion = Request.IsHttps ? $"{sitio}/pagos/webhook" : null
            };

            var (okLink, link, errorLink) = await _api.PostAsync<PreferenciaPagoDto>(
                $"api/Pagos/{creado.Id}/preferencia", pedirLink, ct);

            if (!okLink || link is null)
                return Ok(new { ok = false, error = errorLink ?? "No pudimos iniciar el pago. Probá de nuevo." });

            urlPago = link.UrlPago;
        }

        return Ok(new
        {
            ok = true,
            numero = creado.Numero,
            total = creado.Total,
            urlPago,
            items = creado.Items.Select(i => new
            {
                titulo = i.Titulo,
                precio = i.Precio,
                cantidad = i.Cantidad,
                subtotal = i.Subtotal
            })
        });
    }
}
