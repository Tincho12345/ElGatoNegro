using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrabajosWeb.Api.Data;
using TrabajosWeb.Api.Entities;
using TrabajosWeb.Shared.DTOs;
using TrabajosWeb.Shared.Helpers;

namespace TrabajosWeb.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public CategoriasController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>Categorías activas que tienen al menos un trabajo publicado.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<CategoriaDto>>> GetPublicas(CancellationToken ct)
    {
        var lista = await _context.Categorias
            .AsNoTracking()
            .Where(c => c.Activa && c.Trabajos.Any(t => t.Publicado))
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .ProjectTo<CategoriaDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Ok(lista);
    }

    /// <summary>
    /// Servicios que se ofrecen en la home. A diferencia de GetPublicas, no exige
    /// que haya trabajos cargados: la peluquería ofrece el servicio igual.
    /// </summary>
    [HttpGet("servicios")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CategoriaDto>>> GetServicios(CancellationToken ct)
    {
        var lista = await _context.Categorias
            .AsNoTracking()
            .Where(c => c.Activa && c.MostrarEnServicios)
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .ProjectTo<CategoriaDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Ok(lista);
    }

    [HttpGet("admin")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<List<CategoriaDto>>> GetTodas(CancellationToken ct)
    {
        var lista = await _context.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .ProjectTo<CategoriaDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<CategoriaDto>> GetPorId(Guid id, CancellationToken ct)
    {
        var categoria = await _context.Categorias
            .AsNoTracking()
            .Where(c => c.Id == id)
            .ProjectTo<CategoriaDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

        return categoria is null ? NotFound() : Ok(categoria);
    }

    [HttpPost]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<CategoriaDto>> Crear(CategoriaCreateDto dto, CancellationToken ct)
    {
        var slug = SlugHelper.Generar(dto.Nombre);

        if (await _context.Categorias.AnyAsync(c => c.Slug == slug, ct))
            return Conflict(new { mensaje = "Ya existe una categoría con ese nombre." });

        var categoria = _mapper.Map<Categoria>(dto);
        categoria.Slug = slug;

        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetPorId), new { id = categoria.Id },
            _mapper.Map<CategoriaDto>(categoria));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Actualizar(Guid id, CategoriaCreateDto dto, CancellationToken ct)
    {
        var categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (categoria is null)
            return NotFound();

        var slug = SlugHelper.Generar(dto.Nombre);

        if (await _context.Categorias.AnyAsync(c => c.Slug == slug && c.Id != id, ct))
            return Conflict(new { mensaje = "Ya existe otra categoría con ese nombre." });

        _mapper.Map(dto, categoria);
        categoria.Slug = slug;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var categoria = await _context.Categorias
            .Include(c => c.Trabajos)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (categoria is null)
            return NotFound();

        if (categoria.Trabajos.Any())
        {
            return Conflict(new
            {
                mensaje = $"No se puede eliminar: hay {categoria.Trabajos.Count} trabajo(s) en esta categoría. " +
                          "Reasignalos o desactivá la categoría en su lugar."
            });
        }

        _context.Categorias.Remove(categoria);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}