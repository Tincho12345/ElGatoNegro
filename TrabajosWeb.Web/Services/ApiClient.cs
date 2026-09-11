using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace TrabajosWeb.Web.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<ApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOpciones = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient http, IHttpContextAccessor accessor, ILogger<ApiClient> logger)
    {
        _http = http;
        _accessor = accessor;
        _logger = logger;
    }

    /// <summary>
    /// Adjunta el token guardado en la cookie de autenticación, si existe.
    /// Los endpoints públicos simplemente van sin header.
    /// </summary>
    private void AplicarToken()
    {
        var token = _accessor.HttpContext?.User?.FindFirst("AccessToken")?.Value;

        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(string ruta, CancellationToken ct = default)
    {
        AplicarToken();

        try
        {
            var respuesta = await _http.GetAsync(ruta, ct);

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("GET {Ruta} devolvió {Codigo}", ruta, respuesta.StatusCode);
                return default;
            }

            return await respuesta.Content.ReadFromJsonAsync<T>(JsonOpciones, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo contactar la API en {Ruta}", ruta);
            return default;
        }
    }

    public async Task<(bool Ok, TRespuesta? Datos, string? Error)> PostAsync<TRespuesta>(
        string ruta, object cuerpo, CancellationToken ct = default)
    {
        AplicarToken();

        try
        {
            var respuesta = await _http.PostAsJsonAsync(ruta, cuerpo, ct);

            if (respuesta.IsSuccessStatusCode)
            {
                var datos = await respuesta.Content.ReadFromJsonAsync<TRespuesta>(JsonOpciones, ct);
                return (true, datos, null);
            }

            return (false, default, await LeerErrorAsync(respuesta, ct));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error en POST {Ruta}", ruta);
            return (false, default, "No se pudo conectar con el servidor.");
        }
    }

    public async Task<(bool Ok, string? Error)> PutAsync(string ruta, object cuerpo, CancellationToken ct = default)
    {
        AplicarToken();

        try
        {
            var respuesta = await _http.PutAsJsonAsync(ruta, cuerpo, ct);
            return respuesta.IsSuccessStatusCode
                ? (true, null)
                : (false, await LeerErrorAsync(respuesta, ct));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error en PUT {Ruta}", ruta);
            return (false, "No se pudo conectar con el servidor.");
        }
    }

    public async Task<(bool Ok, string? Error)> PatchAsync(string ruta, CancellationToken ct = default)
    {
        AplicarToken();

        try
        {
            var respuesta = await _http.PatchAsync(ruta, null, ct);
            return respuesta.IsSuccessStatusCode
                ? (true, null)
                : (false, await LeerErrorAsync(respuesta, ct));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error en PATCH {Ruta}", ruta);
            return (false, "No se pudo conectar con el servidor.");
        }
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(string ruta, CancellationToken ct = default)
    {
        AplicarToken();

        try
        {
            var respuesta = await _http.DeleteAsync(ruta, ct);
            return respuesta.IsSuccessStatusCode
                ? (true, null)
                : (false, await LeerErrorAsync(respuesta, ct));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error en DELETE {Ruta}", ruta);
            return (false, "No se pudo conectar con el servidor.");
        }
    }

    public async Task<(bool Ok, string? Error)> PostArchivosAsync(
        string ruta, IEnumerable<IFormFile> archivos, CancellationToken ct = default)
    {
        AplicarToken();

        using var contenido = new MultipartFormDataContent();

        foreach (var archivo in archivos)
        {
            var stream = archivo.OpenReadStream();
            var parte = new StreamContent(stream);
            parte.Headers.ContentType = new MediaTypeHeaderValue(archivo.ContentType);

            // El nombre "archivos" tiene que coincidir con el parámetro del MediaController
            contenido.Add(parte, "archivos", archivo.FileName);
        }

        try
        {
            var respuesta = await _http.PostAsync(ruta, contenido, ct);
            return respuesta.IsSuccessStatusCode
                ? (true, null)
                : (false, await LeerErrorAsync(respuesta, ct));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error subiendo archivos a {Ruta}", ruta);
            return (false, "No se pudo conectar con el servidor.");
        }
    }

    private static async Task<string> LeerErrorAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            return "Tu sesión expiró. Iniciá sesión de nuevo.";

        try
        {
            var json = await respuesta.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("mensaje", out var mensaje))
                return mensaje.GetString() ?? "Ocurrió un error.";
        }
        catch (JsonException) { }

        return $"Error {(int)respuesta.StatusCode}.";
    }
}