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

    [HttpPost("confirmar")]
    public async Task<IActionResult> Confirmar([FromBody] PedidoCreateDto dto, CancellationToken ct)
    {
        var (ok, creado, error) = await _api.PostAsync<PedidoDto>("api/Pedidos", dto, ct);

        if (!ok || creado is null)
            return Ok(new { ok = false, error = error ?? "No se pudo registrar el pedido." });

        return Ok(new
        {
            ok = true,
            numero = creado.Numero,
            total = creado.Total,
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
