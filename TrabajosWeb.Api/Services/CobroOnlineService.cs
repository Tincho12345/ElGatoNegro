using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrabajosWeb.Api.Configuration;
using TrabajosWeb.Api.Data;

namespace TrabajosWeb.Api.Services;

public interface ICobroOnlineService
{
    /// <summary>Si el carrito puede ofrecer Mercado Pago ahora mismo.</summary>
    Task<bool> MercadoPagoHabilitadoAsync(CancellationToken ct = default);
}

public class CobroOnlineService : ICobroOnlineService
{
    private readonly AppDbContext _context;
    private readonly MercadoPagoSettings _settings;

    public CobroOnlineService(AppDbContext context, IOptions<MercadoPagoSettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    /// <summary>Prendido en el panel y con el Access Token cargado: sin token no hay cobro posible.</summary>
    public async Task<bool> MercadoPagoHabilitadoAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccessToken))
            return false;

        return await _context.AjustesSitio
            .AsNoTracking()
            .AnyAsync(a => a.CobroOnlineMercadoPago, ct);
    }
}
