using Microsoft.Extensions.Options;
using TrabajosWeb.Api.Configuration;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Api.Services;

public class FileStorageService : IFileStorageService
{
    private readonly UploadSettings _settings;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(
        IOptions<UploadSettings> settings,
        IWebHostEnvironment env,
        ILogger<FileStorageService> logger)
    {
        _settings = settings.Value;
        _env = env;
        _logger = logger;
    }

    private string RaizFisica =>
        Path.Combine(_env.WebRootPath ?? "wwwroot", _settings.Carpeta);

    public async Task<ArchivoGuardado> GuardarAsync(IFormFile archivo, CancellationToken ct = default)
    {
        if (archivo is null || archivo.Length == 0)
            throw new InvalidOperationException("El archivo está vacío.");

        if (archivo.Length > _settings.TamanoMaximoBytes)
            throw new InvalidOperationException(
                $"El archivo supera el máximo permitido de {_settings.TamanoMaximoMb} MB.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

        TipoMedia tipo;
        if (_settings.ExtensionesImagen.Contains(extension))
            tipo = TipoMedia.Imagen;
        else if (_settings.ExtensionesVideo.Contains(extension))
            tipo = TipoMedia.Video;
        else
            throw new InvalidOperationException($"Extensión no permitida: {extension}");

        // Subcarpeta por año/mes para no juntar miles de archivos en un solo directorio
        var subcarpeta = DateTime.UtcNow.ToString("yyyy/MM");
        var carpetaDestino = Path.Combine(RaizFisica, subcarpeta.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(carpetaDestino);

        // Nombre generado: nunca confiar en el nombre original del cliente
        var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
        var rutaFisica = Path.Combine(carpetaDestino, nombreArchivo);

        await using (var stream = new FileStream(rutaFisica, FileMode.Create))
        {
            await archivo.CopyToAsync(stream, ct);
        }

        var rutaRelativa = $"/{_settings.Carpeta}/{subcarpeta}/{nombreArchivo}";
        _logger.LogInformation("Archivo guardado en {Ruta}", rutaRelativa);

        return new ArchivoGuardado(rutaRelativa, tipo);
    }

    public void Eliminar(string rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa)) return;

        var relativoLimpio = rutaRelativa.TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        var raiz = Path.GetFullPath(_env.WebRootPath ?? "wwwroot");
        var rutaFisica = Path.GetFullPath(Path.Combine(raiz, relativoLimpio));

        // Defensa contra path traversal: el destino debe seguir dentro de wwwroot
        if (!rutaFisica.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Intento de eliminar fuera de wwwroot: {Ruta}", rutaRelativa);
            return;
        }

        if (File.Exists(rutaFisica))
            File.Delete(rutaFisica);
    }
}