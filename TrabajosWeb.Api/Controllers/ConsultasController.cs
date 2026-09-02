using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrabajosWeb.Api.Data;
using TrabajosWeb.Api.Entities;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsultasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ConsultasController> _logger;

    public ConsultasController(
        AppDbContext context,
        IMapper mapper,
        ILogger<ConsultasController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Alta pública desde el formulario del sitio.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Crear(ConsultaCreateDto dto, CancellationToken ct)
    {
        var consulta = _mapper.Map<Consulta>(dto);
        consulta.CreatedDate = DateTime.UtcNow;

        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Nueva consulta recibida (Id {Id})", consulta.Id);

        // No se devuelve el objeto: el visitante no necesita ver el Id ni el resto.
        return Ok(new { mensaje = "Consulta enviada correctamente." });
    }

    [HttpGet]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<ActionResult<List<ConsultaDto>>> GetTodas(
        [FromQuery] bool? soloNoLeidas,
        CancellationToken ct)
    {
        var query = _context.Consultas.AsNoTracking().AsQueryable();

        if (soloNoLeidas == true)
            query = query.Where(c => !c.Leida);

        var lista = await query
            .OrderByDescending(c => c.CreatedDate)
            .ProjectTo<ConsultaDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return Ok(lista);
    }

    [HttpPatch("{id:guid}/leida")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> MarcarLeida(Guid id, CancellationToken ct)
    {
        var consulta = await _context.Consultas.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (consulta is null)
            return NotFound();

        consulta.Leida = true;
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        var consulta = await _context.Consultas.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (consulta is null)
            return NotFound();

        _context.Consultas.Remove(consulta);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}