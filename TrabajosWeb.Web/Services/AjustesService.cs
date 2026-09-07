using Microsoft.Extensions.Caching.Memory;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Web.Services;

public interface IAjustesService
{
    /// <summary>Datos del sitio. Se cachean unos minutos: los pide cada página.</summary>
    Task<AjustesSitioDto> ObtenerAsync(CancellationToken ct = default);

    /// <summary>Borra el caché para que el próximo pedido traiga los datos nuevos.</summary>
    void Invalidar();
}

public class AjustesService : IAjustesService
{
    private const string Clave = "ajustes-sitio";
    private static readonly TimeSpan Duracion = TimeSpan.FromMinutes(5);

    private readonly IApiClient _api;
    private readonly IMemoryCache _cache;

    public AjustesService(IApiClient api, IMemoryCache cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<AjustesSitioDto> ObtenerAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(Clave, out AjustesSitioDto? guardado) && guardado is not null)
            return guardado;

        // Si la API no responde, devolvemos un objeto vacío: la web se muestra
        // igual, solo sin los datos de contacto.
        var ajustes = await _api.GetAsync<AjustesSitioDto>("api/Ajustes", ct)
            ?? new AjustesSitioDto { NombreSitio = "El Gato Negro" };

        _cache.Set(Clave, ajustes, Duracion);

        return ajustes;
    }

    public void Invalidar() => _cache.Remove(Clave);
}