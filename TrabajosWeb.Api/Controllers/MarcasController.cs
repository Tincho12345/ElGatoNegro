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
public class MarcasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public MarcasController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>Marcas activas con al menos un producto publicado.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<MarcaDto>>> GetPublicas(CancellationToken ct)
    {
        var lista = await _context.Marcas
            .AsNoTracking()
            .Where(m => m.Activa && m.Trabajos.Any(t => t.Publicado))
            .OrderBy(m => m.Orden).ThenBy(m => m.Nombre)
            .ProjectTo<MarcaDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Ok(lista);
    }

    [HttpGet("admin")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<List<MarcaDto>>> GetTodas(CancellationToken ct)
    {
        var lista = await _context.Marcas
            .AsNoTracking()
            .OrderBy(m => m.Orden).ThenBy(m => m.Nombre)
            .ProjectTo<MarcaDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<MarcaDto>> GetPorId(Guid id, CancellationToken ct)
    {
        var marca = await _context.Marcas
            .AsNoTracking()
            .Where(m => m.Id == id)
            .ProjectTo<MarcaDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);

        return marca is null ? NotFound() : Ok(marca);
    }

    [HttpPost]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<MarcaDto>> Crear(MarcaCreateDto dto, CancellationToken ct)
    {
        var slug = SlugHelper.Generar(dto.Nombre);

        if (await _context.Marcas.AnyAsync(m => m.Slug == slug, ct))
            return Conflict(new { mensaje = "Ya existe una marca con ese nombre." });

        var marca = _mapper.Map<Marca>(dto);
        marca.Slug = slug;

        _context.Marcas.Add(marca);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetPorId), new { id = marca.Id },
            _mapper.Map<MarcaDto>(marca));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Actualizar(Guid id, MarcaCreateDto dto, CancellationToken ct)
    {
        var marca = await _context.Marcas.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (marca is null)
            return NotFound();

        var slug = SlugHelper.Generar(dto.Nombre);

        if (await _context.Marcas.AnyAsync(m => m.Slug == slug && m.Id != id, ct))
            return Conflict(new { mensaje = "Ya existe otra marca con ese nombre." });

        _mapper.Map(dto, marca);
        marca.Slug = slug;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var marca = await _context.Marcas
            .Include(m => m.Trabajos)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (marca is null)
            return NotFound();

        if (marca.Trabajos.Any())
        {
            return Conflict(new
            {
                mensaje = $"No se puede eliminar: hay {marca.Trabajos.Count} producto(s) de esta marca. " +
                          "Reasignalos o desactivá la marca en su lugar."
            });
        }

        _context.Marcas.Remove(marca);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}