using TrabajosWeb.Api.Entities;

namespace TrabajosWeb.Api.Services;

public record PagoMercadoPago(string Id, string Estado, string? ReferenciaExterna, decimal Monto);

public interface IMercadoPagoService
{
    /// <summary>Crea el link de pago del pedido. Null si Mercado Pago no lo pudo crear.</summary>
    Task<string?> CrearPreferenciaAsync(
        Pedido pedido, string urlRetorno, string? urlNotificacion, CancellationToken ct = default);

    /// <summary>
    /// Pide el pago directamente a Mercado Pago. Es la única fuente confiable:
    /// lo que llega por el aviso (webhook) o por la URL de retorno no se toma
    /// como cierto hasta confirmarlo acá.
    /// </summary>
    Task<PagoMercadoPago?> ObtenerPagoAsync(string paymentId, CancellationToken ct = default);
}
