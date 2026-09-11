using AutoMapper;
using AutoMapper.QueryableExtensions;
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
public class TrabajosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _storage;

    public TrabajosController(AppDbContext context, IMapper mapper, IFileStorageService storage)
    {
        _context = context;
        _mapper = mapper;
        _storage = storage;
    }

    /// <summary>
    /// Galería pública. Todos los filtros son opcionales: sin ninguno devuelve
    /// lo mismo de siempre. Subcategoría, marca y precio solo tienen sentido
    /// dentro de Productos.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<TrabajoDto>>> GetPublicados(
        [FromQuery] string? categoria,
        [FromQuery] string? subcategoria,
        [FromQuery] string? marca,
        [FromQuery] decimal? precioMin,
        [FromQuery] decimal? precioMax,
        [FromQuery] bool? enOferta,
        CancellationToken ct)
    {
        var query = _context.Trabajos
            .AsNoTracking()
            .Where(t => t.Publicado);

        if (!string.IsNullOrWhiteSpace(categoria))
            query = query.Where(t => t.Categoria!.Slug == categoria);

        if (!string.IsNullOrWhiteSpace(subcategoria))
            query = query.Where(t => t.Subcategoria != null && t.Subcategoria.Slug == subcategoria);

        if (!string.IsNullOrWhiteSpace(marca))
            query = query.Where(t => t.Marca != null && t.Marca.Slug == marca);

        // Filtrar por precio descarta lo que no tiene precio cargado:
        // un trabajo sin precio no pertenece a ningún rango.
        if (precioMin.HasValue)
            query = query.Where(t => t.Precio >= precioMin.Value);

        if (precioMax.HasValue)
            query = query.Where(t => t.Precio <= precioMax.Value);

        if (enOferta == true)
        {
            query = query.Where(t =>
                (t.PrecioAnterior != null && t.Precio != null && t.PrecioAnterior > t.Precio)
                || (t.EtiquetaOferta != null && t.EtiquetaOferta != "")
                || (t.TextoOferta != null && t.TextoOferta != ""));
        }

        var lista = await query
            .OrderByDescending(t => t.Destacado)
            .ThenByDescending(t => t.CreatedDate)
            .ProjectTo<TrabajoDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Ok(lista);
    }

    /// <summary>
    /// Precio más bajo y más alto de los productos publicados de una categoría.
    /// Sirve para armar el control de rango sin inventar los topes.
    /// </summary>
    [HttpGet("rango-precios")]
    [AllowAnonymous]
    public async Task<ActionResult<RangoPreciosDto>> GetRangoPrecios(
        [FromQuery] string? categoria,
        CancellationToken ct)
    {
        var query = _context.Trabajos
            .AsNoTracking()
            .Where(t => t.Publicado && t.Precio != null);

        if (!string.IsNullOrWhiteSpace(categoria))
            query = query.Where(t => t.Categoria!.Slug == categoria);

        var precios = await query
            .Select(t => t.Precio!.Value)
            .ToListAsync(ct);

        if (precios.Count == 0)
            return Ok(new RangoPreciosDto());

        return Ok(new RangoPreciosDto
        {
            Minimo = precios.Min(),
            Maximo = precios.Max()
        });
    }

    /// <summary>Listado completo para el panel admin, incluidos los no publicados.</summary>
    [HttpGet("admin")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<List<TrabajoDto>>> GetTodos(CancellationToken ct)
    {
        var lista = await _context.Trabajos
            .AsNoTracking()
            .OrderByDescending(t => t.Destacado)
            .ThenByDescending(t => t.CreatedDate)
            .ProjectTo<TrabajoDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TrabajoDto>> GetPorId(Guid id, CancellationToken ct)
    {
        var trabajo = await _context.Trabajos
            .AsNoTracking()
            .Where(t => t.Id == id)
            .ProjectTo<TrabajoDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

        if (trabajo is null)
            return NotFound();

        // Un trabajo despublicado solo lo ve el admin
        if (!trabajo.Publicado && !User.IsInRole("Admin"))
            return NotFound();

        return Ok(trabajo);
    }

    [HttpPost]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<TrabajoDto>> Crear(TrabajoCreateDto dto, CancellationToken ct)
    {
        var trabajo = _mapper.Map<Trabajo>(dto);
        trabajo.CreatedDate = DateTime.UtcNow;

        _context.Trabajos.Add(trabajo);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetPorId), new { id = trabajo.Id },
            _mapper.Map<TrabajoDto>(trabajo));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Actualizar(Guid id, TrabajoCreateDto dto, CancellationToken ct)
    {
        var trabajo = await _context.Trabajos.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (trabajo is null)
            return NotFound();

        _mapper.Map(dto, trabajo);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var trabajo = await _context.Trabajos
            .Include(t => t.Medios)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (trabajo is null)
            return NotFound();

        // Los archivos físicos se borran antes: el cascade solo limpia la base
        foreach (var media in trabajo.Medios)
            _storage.Eliminar(media.RutaRelativa);

        _context.Trabajos.Remove(trabajo);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}