namespace TrabajosWeb.Web.Services;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string ruta, CancellationToken ct = default);
    Task<(bool Ok, TRespuesta? Datos, string? Error)> PostAsync<TRespuesta>(string ruta, object cuerpo, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> PutAsync(string ruta, object cuerpo, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> PatchAsync(string ruta, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> DeleteAsync(string ruta, CancellationToken ct = default);
    Task<(bool Ok, string? Error)> PostArchivosAsync(string ruta, IEnumerable<IFormFile> archivos, CancellationToken ct = default);
}