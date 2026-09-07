using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrabajosWeb.Api.Data;
using TrabajosWeb.Api.Entities;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AjustesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public AjustesController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>Los datos del sitio. Si todavía no existe la fila, se crea con valores por defecto.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<AjustesSitioDto>> Get(CancellationToken ct)
    {
        var ajustes = await ObtenerOCrearAsync(ct);
        return Ok(_mapper.Map<AjustesSitioDto>(ajustes));
    }

    [HttpPut]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Actualizar(AjustesSitioUpdateDto dto, CancellationToken ct)
    {
        var ajustes = await ObtenerOCrearAsync(ct);

        _mapper.Map(dto, ajustes);

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// La tabla tiene una sola fila. Nadie la crea a mano: si falta, se genera acá
    /// con los valores por defecto de la entidad.
    /// </summary>
    private async Task<AjustesSitio> ObtenerOCrearAsync(CancellationToken ct)
    {
        var ajustes = await _context.AjustesSitio.FirstOrDefaultAsync(ct);

        if (ajustes is not null)
            return ajustes;

        ajustes = new AjustesSitio();

        _context.AjustesSitio.Add(ajustes);
        await _context.SaveChangesAsync(ct);

        return ajustes;
    }
}