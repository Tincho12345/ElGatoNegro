using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrabajosWeb.Api.Data;
using TrabajosWeb.Api.Entities;
using TrabajosWeb.Api.Services;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPushNotificationService _push;
    private readonly ICobroOnlineService _cobroOnline;
    private readonly ILogger<PedidosController> _logger;

    public PedidosController(
        AppDbContext context,
        IPushNotificationService push,
        ICobroOnlineService cobroOnline,
        ILogger<PedidosController> logger)
    {
        _context = context;
        _push = push;
        _cobroOnline = cobroOnline;
        _logger = logger;
    }

    /// <summary>
    /// Alta pública desde el carrito del sitio. El precio nunca viene del
    /// cliente: cada línea se recalcula acá a partir del Trabajo actual, para
    /// que nadie pueda mandar un precio manipulado desde el navegador.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Crear(PedidoCreateDto dto, CancellationToken ct)
    {
        if (dto.Items is null || dto.Items.Count == 0)
            return BadRequest(new { mensaje = "El carrito está vacío." });

        if (dto.MetodoPago == MetodoPago.MercadoPago && !await _cobroOnline.MercadoPagoHabilitadoAsync(ct))
            return BadRequest(new { mensaje = "Por ahora el pago online no está disponible. Podés coordinar el pago con el salón." });

        var idsPedidos = dto.Items.Select(i => i.TrabajoId).Distinct().ToList();

        var trabajos = await _context.Trabajos
            .Where(t => idsPedidos.Contains(t.Id) && t.Publicado)
            .ToDictionaryAsync(t => t.Id, ct);

        var items = new List<PedidoItem>();

        foreach (var itemDto in dto.Items)
        {
            if (!trabajos.TryGetValue(itemDto.TrabajoId, out var trabajo) || trabajo.Precio is null)
                continue;

            var cantidad = Math.Clamp(itemDto.Cantidad, 1, 99);
            var precio = trabajo.Precio.Value;

            items.Add(new PedidoItem
            {
                TrabajoId = trabajo.Id,
                Titulo = trabajo.Titulo,
                Precio = precio,
                Cantidad = cantidad,
                Subtotal = precio * cantidad
            });
        }

        if (items.Count == 0)
            return BadRequest(new { mensaje = "Ninguno de los productos del carrito está disponible." });

        var siguienteNumero = (await _context.Pedidos.MaxAsync(p => (int?)p.Numero, ct) ?? 0) + 1;

        var pagaOnline = dto.MetodoPago == MetodoPago.MercadoPago;

        var pedido = new Pedido
        {
            Numero = siguienteNumero,
            Nombre = dto.Nombre,
            Telefono = dto.Telefono,
            Nota = dto.Nota,
            Total = items.Sum(i => i.Subtotal),
            MetodoPago = dto.MetodoPago,
            EstadoPago = pagaOnline ? EstadoPago.Pendiente : EstadoPago.SinPago,
            // El pago online se avisa recién cuando Mercado Pago lo aprueba
            // (ver PagosController): hasta entonces no es una novedad del panel.
            Leido = pagaOnline,
            Items = items
        };

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Nuevo pedido recibido (N° {Numero})", pedido.Numero);

        // Va después de guardar y con su propio try/catch adentro del
        // servicio: si falla el envío, el pedido ya quedó guardado igual.
        // consultaId queda en null a propósito: ese parámetro es específico
        // de las consultas del formulario de contacto, no de los pedidos.
        if (!pagaOnline)
        {
            await _push.NotificarATodosAsync(
                "Nuevo pedido",
                $"{pedido.Nombre}: pedido N° {pedido.Numero} — {pedido.Total.ToString("C0")}",
                "/Admin/Pedidos",
                ct: ct);
        }

        return Ok(MapearDto(pedido));
    }

    [HttpGet]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<List<PedidoDto>>> GetTodos(
        [FromQuery] bool? soloNoLeidos,
        CancellationToken ct)
    {
        var query = _context.Pedidos.AsNoTracking().Include(p => p.Items).AsQueryable();

        if (soloNoLeidos == true)
            query = query.Where(p => !p.Leido);

        var lista = await query
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync(ct);

        return Ok(lista.Select(MapearDto).ToList());
    }

    /// <summary>
    /// Listado del panel con filtros y paginado. También lo usa la exportación
    /// a Excel, pidiendo Tamano = 0 para traer todo lo que cumple los filtros.
    /// </summary>
    [HttpGet("buscar")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<PedidosPaginaDto>> Buscar(
        [FromQuery] PedidoFiltroDto filtro,
        CancellationToken ct)
    {
        var query = _context.Pedidos.AsNoTracking().AsQueryable();

        var texto = filtro.Texto?.Trim();
        if (!string.IsNullOrEmpty(texto))
        {
            var esNumero = int.TryParse(texto, out var numero);

            query = query.Where(p =>
                p.Nombre.Contains(texto)
                || p.Telefono.Contains(texto)
                || (p.Nota != null && p.Nota.Contains(texto))
                || p.Items.Any(i => i.Titulo.Contains(texto))
                || (esNumero && p.Numero == numero));
        }

        // Las fechas del filtro son días del calendario local: el día "hasta" entra completo.
        if (filtro.Desde.HasValue)
        {
            var desde = filtro.Desde.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
            query = query.Where(p => p.CreatedDate >= desde);
        }

        if (filtro.Hasta.HasValue)
        {
            var hasta = filtro.Hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
            query = query.Where(p => p.CreatedDate < hasta);
        }

        if (filtro.Leido.HasValue)
            query = query.Where(p => p.Leido == filtro.Leido.Value);

        if (filtro.EstadoPago.HasValue)
            query = query.Where(p => p.EstadoPago == filtro.EstadoPago.Value);

        var total = await query.CountAsync(ct);
        var importeTotal = await query.SumAsync(p => (decimal?)p.Total, ct) ?? 0m;

        var ordenada = query
            .Include(p => p.Items)
            .OrderByDescending(p => p.CreatedDate);

        var pagina = Math.Max(filtro.Pagina, 1);
        var tamano = Math.Clamp(filtro.Tamano, 0, 100);
        var totalPaginas = 1;

        List<Pedido> pedidos;

        if (tamano == 0)
        {
            pedidos = await ordenada.ToListAsync(ct);
        }
        else
        {
            totalPaginas = Math.Max((int)Math.Ceiling(total / (double)tamano), 1);
            pagina = Math.Min(pagina, totalPaginas);

            pedidos = await ordenada
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .ToListAsync(ct);
        }

        return Ok(new PedidosPaginaDto
        {
            Items = pedidos.Select(MapearDto).ToList(),
            Total = total,
            ImporteTotal = importeTotal,
            Pagina = pagina,
            TotalPaginas = totalPaginas
        });
    }

    [HttpPatch("{id:guid}/leido")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> MarcarLeido(Guid id, CancellationToken ct)
    {
        var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null)
            return NotFound();

        pedido.Leido = true;
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pedido is null)
            return NotFound();

        _context.Pedidos.Remove(pedido);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }

    private static PedidoDto MapearDto(Pedido pedido) => new()
    {
        Id = pedido.Id,
        Numero = pedido.Numero,
        Nombre = pedido.Nombre,
        Telefono = pedido.Telefono,
        Nota = pedido.Nota,
        Total = pedido.Total,
        Leido = pedido.Leido,
        MetodoPago = pedido.MetodoPago,
        EstadoPago = pedido.EstadoPago,
        PagadoEl = pedido.PagadoEl,
        CreatedDate = pedido.CreatedDate,
        Items = pedido.Items.Select(i => new PedidoItemDto
        {
            Id = i.Id,
            TrabajoId = i.TrabajoId,
            Titulo = i.Titulo,
            Precio = i.Precio,
            Cantidad = i.Cantidad,
            Subtotal = i.Subtotal
        }).ToList()
    };
}
