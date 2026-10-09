using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TrabajosWeb.Api.Configuration;
using TrabajosWeb.Api.Entities;

namespace TrabajosWeb.Api.Services;

public class MercadoPagoService : IMercadoPagoService
{
    private readonly HttpClient _http;
    private readonly MercadoPagoSettings _settings;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(
        HttpClient http,
        IOptions<MercadoPagoSettings> settings,
        ILogger<MercadoPagoService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string?> CrearPreferenciaAsync(
        Pedido pedido, string urlRetorno, string? urlNotificacion, CancellationToken ct = default)
    {
        if (!HayCredenciales()) return null;

        var retorno = $"{urlRetorno}?pedido={pedido.Id}";

        var cuerpo = new Dictionary<string, object?>
        {
            ["items"] = pedido.Items.Select(i => new
            {
                id = i.TrabajoId?.ToString() ?? i.Id.ToString(),
                title = i.Titulo,
                quantity = i.Cantidad,
                unit_price = i.Precio,
                currency_id = "ARS"
            }).ToList(),
            ["payer"] = new { name = pedido.Nombre },
            ["external_reference"] = pedido.Id.ToString(),
            ["statement_descriptor"] = _settings.DescriptorResumen,
            ["back_urls"] = new { success = retorno, failure = retorno, pending = retorno }
        };

        // auto_return exige que la vuelta sea https; en local no se manda.
        if (retorno.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            cuerpo["auto_return"] = "approved";

        if (!string.IsNullOrWhiteSpace(urlNotificacion))
            cuerpo["notification_url"] = urlNotificacion;

        using var peticion = NuevaPeticion(HttpMethod.Post, "checkout/preferences");
        peticion.Content = JsonContent.Create(cuerpo);

        try
        {
            using var respuesta = await _http.SendAsync(peticion, ct);
            var json = await respuesta.Content.ReadAsStringAsync(ct);

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogError("Mercado Pago rechazó la preferencia del pedido {Numero}: {Codigo} {Cuerpo}",
                    pedido.Numero, (int)respuesta.StatusCode, json);
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("init_point", out var url) ? url.GetString() : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "No se pudo crear la preferencia de pago del pedido {Numero}", pedido.Numero);
            return null;
        }
    }

    public async Task<PagoMercadoPago?> ObtenerPagoAsync(string paymentId, CancellationToken ct = default)
    {
        if (!HayCredenciales()) return null;

        using var peticion = NuevaPeticion(HttpMethod.Get, $"v1/payments/{Uri.EscapeDataString(paymentId)}");

        try
        {
            using var respuesta = await _http.SendAsync(peticion, ct);

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("Mercado Pago devolvió {Codigo} al consultar el pago {PaymentId}",
                    (int)respuesta.StatusCode, paymentId);
                return null;
            }

            using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            var raiz = doc.RootElement;

            return new PagoMercadoPago(
                Id: raiz.GetProperty("id").ToString(),
                Estado: raiz.GetProperty("status").GetString() ?? string.Empty,
                ReferenciaExterna: raiz.TryGetProperty("external_reference", out var r) ? r.GetString() : null,
                Monto: raiz.TryGetProperty("transaction_amount", out var m) && m.ValueKind == JsonValueKind.Number
                    ? decimal.Parse(m.GetRawText(), CultureInfo.InvariantCulture)
                    : 0m);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            _logger.LogError(ex, "No se pudo consultar el pago {PaymentId}", paymentId);
            return null;
        }
    }

    private bool HayCredenciales()
    {
        if (!string.IsNullOrWhiteSpace(_settings.AccessToken)) return true;

        _logger.LogError("Falta MercadoPago:AccessToken en appsettings.json de la API.");
        return false;
    }

    private HttpRequestMessage NuevaPeticion(HttpMethod metodo, string ruta)
    {
        var peticion = new HttpRequestMessage(metodo, ruta);
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
        return peticion;
    }
}
