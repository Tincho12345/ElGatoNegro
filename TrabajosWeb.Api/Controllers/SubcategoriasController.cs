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
public class SubcategoriasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public SubcategoriasController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>Subcategorías activas con al menos un producto publicado.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<SubcategoriaDto>>> GetPublicas(CancellationToken ct)
    {
        var lista = await _context.Subcategorias
            .AsNoTracking()
            .Where(s => s.Activa && s.Trabajos.Any(t => t.Publicado))
            .OrderBy(s => s.Orden).ThenBy(s => s.Nombre)
            .ProjectTo<SubcategoriaDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Ok(lista);
    }

    [HttpGet("admin")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<List<SubcategoriaDto>>> GetTodas(CancellationToken ct)
    {
        var lista = await _context.Subcategorias
            .AsNoTracking()
            .OrderBy(s => s.Orden).ThenBy(s => s.Nombre)
            .ProjectTo<SubcategoriaDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<SubcategoriaDto>> GetPorId(Guid id, CancellationToken ct)
    {
        var subcategoria = await _context.Subcategorias
            .AsNoTracking()
            .Where(s => s.Id == id)
            .ProjectTo<SubcategoriaDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

        return subcategoria is null ? NotFound() : Ok(subcategoria);
    }

    [HttpPost]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<SubcategoriaDto>> Crear(SubcategoriaCreateDto dto, CancellationToken ct)
    {
        var slug = SlugHelper.Generar(dto.Nombre);

        if (await _context.Subcategorias.AnyAsync(s => s.Slug == slug, ct))
            return Conflict(new { mensaje = "Ya existe una subcategoría con ese nombre." });

        var subcategoria = _mapper.Map<Subcategoria>(dto);
        subcategoria.Slug = slug;

        _context.Subcategorias.Add(subcategoria);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetPorId), new { id = subcategoria.Id },
            _mapper.Map<SubcategoriaDto>(subcategoria));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Actualizar(Guid id, SubcategoriaCreateDto dto, CancellationToken ct)
    {
        var subcategoria = await _context.Subcategorias.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (subcategoria is null)
            return NotFound();

        var slug = SlugHelper.Generar(dto.Nombre);

        if (await _context.Subcategorias.AnyAsync(s => s.Slug == slug && s.Id != id, ct))
            return Conflict(new { mensaje = "Ya existe otra subcategoría con ese nombre." });

        _mapper.Map(dto, subcategoria);
        subcategoria.Slug = slug;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var subcategoria = await _context.Subcategorias
            .Include(s => s.Trabajos)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (subcategoria is null)
            return NotFound();

        if (subcategoria.Trabajos.Any())
        {
            return Conflict(new
            {
                mensaje = $"No se puede eliminar: hay {subcategoria.Trabajos.Count} producto(s) en esta subcategoría. " +
                          "Reasignalos o desactivá la subcategoría en su lugar."
            });
        }

        _context.Subcategorias.Remove(subcategoria);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}