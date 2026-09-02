using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Api.Services;

public record ArchivoGuardado(string RutaRelativa, TipoMedia Tipo);

public interface IFileStorageService
{
    Task<ArchivoGuardado> GuardarAsync(IFormFile archivo, CancellationToken ct = default);
    void Eliminar(string rutaRelativa);
}