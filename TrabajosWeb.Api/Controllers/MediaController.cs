using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrabajosWeb.Api.Data;
using TrabajosWeb.Api.Entities;
using TrabajosWeb.Api.Services;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Api.Controllers;

[ApiController]
[Route("api/trabajos/{trabajoId:guid}/media")]
[Authorize(Policy = "SoloAdmin")]
public class MediaController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _storage;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        AppDbContext context,
        IMapper mapper,
        IFileStorageService storage,
        ILogger<MediaController> logger)
    {
        _context = context;
        _mapper = mapper;
        _storage = storage;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<List<MediaDto>>> Subir(
        Guid trabajoId,
        [FromForm] List<IFormFile> archivos,
        CancellationToken ct)
    {
        if (archivos is null || archivos.Count == 0)
            return BadRequest(new { mensaje = "No se envió ningún archivo." });

        var existe = await _context.Trabajos.AnyAsync(t => t.Id == trabajoId, ct);
        if (!existe)
            return NotFound(new { mensaje = "El trabajo no existe." });

        var ordenBase = await _context.Medios
            .Where(m => m.TrabajoId == trabajoId)
            .Select(m => (int?)m.Orden)
            .MaxAsync(ct) ?? 0;

        var guardados = new List<Media>();
        var rutasEscritas = new List<string>();

        try
        {
            foreach (var archivo in archivos)
            {
                var resultado = await _storage.GuardarAsync(archivo, ct);
                rutasEscritas.Add(resultado.RutaRelativa);

                var media = new Media
                {
                    TrabajoId = trabajoId,
                    Tipo = resultado.Tipo,
                    RutaRelativa = resultado.RutaRelativa,
                    TextoAlternativo = Path.GetFileNameWithoutExtension(archivo.FileName),
                    Orden = ++ordenBase
                };

                _context.Medios.Add(media);
                guardados.Add(media);
            }

            await _context.SaveChangesAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            // Si falla a mitad de camino, se limpian los archivos ya escritos
            // para no dejar huérfanos en disco.
            foreach (var ruta in rutasEscritas)
                _storage.Eliminar(ruta);

            return BadRequest(new { mensaje = ex.Message });
        }

        return Ok(_mapper.Map<List<MediaDto>>(guardados));
    }

    [HttpDelete("{mediaId:guid}")]
    public async Task<IActionResult> Eliminar(Guid trabajoId, Guid mediaId, CancellationToken ct)
    {
        var media = await _context.Medios
            .FirstOrDefaultAsync(m => m.Id == mediaId && m.TrabajoId == trabajoId, ct);

        if (media is null)
            return NotFound();

        _storage.Eliminar(media.RutaRelativa);
        _context.Medios.Remove(media);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPut("orden")]
    public async Task<IActionResult> Reordenar(
        Guid trabajoId,
        [FromBody] List<Guid> idsEnOrden,
        CancellationToken ct)
    {
        var medios = await _context.Medios
            .Where(m => m.TrabajoId == trabajoId)
            .ToListAsync(ct);

        for (var i = 0; i < idsEnOrden.Count; i++)
        {
            var media = medios.FirstOrDefault(m => m.Id == idsEnOrden[i]);
            if (media is not null)
                media.Orden = i + 1;
        }

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}