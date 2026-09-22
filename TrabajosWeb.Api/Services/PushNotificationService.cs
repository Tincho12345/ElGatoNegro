using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrabajosWeb.Api.Configuration;
using TrabajosWeb.Api.Data;
using TrabajosWeb.Api.Entities;
using WebPush;

namespace TrabajosWeb.Api.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _context;
    private readonly VapidSettings _vapid;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        AppDbContext context,
        IOptions<VapidSettings> vapid,
        ILogger<PushNotificationService> logger)
    {
        _context = context;
        _vapid = vapid.Value;
        _logger = logger;
    }

    public async Task NotificarATodosAsync(
        string titulo,
        string cuerpo,
        string url,
        Guid? consultaId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_vapid.PublicKey) || string.IsNullOrWhiteSpace(_vapid.PrivateKey))
        {
            _logger.LogWarning("Notificación push omitida: faltan configurar las claves VAPID.");
            return;
        }

        var suscripciones = await _context.SuscripcionesPush.ToListAsync(ct);
        if (suscripciones.Count == 0)
            return;

        var vapidDetails = new VapidDetails(_vapid.Subject, _vapid.PublicKey, _vapid.PrivateKey);
        var cliente = new WebPushClient();

        var payload = JsonSerializer.Serialize(new { title = titulo, body = cuerpo, url, consultaId });
        var vencidas = new List<SuscripcionPush>();

        foreach (var suscripcion in suscripciones)
        {
            try
            {
                var pushSub = new PushSubscription(suscripcion.Endpoint, suscripcion.P256dh, suscripcion.Auth);
                await cliente.SendNotificationAsync(pushSub, payload, vapidDetails, ct);
            }
            catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                // El navegador invalidó esta suscripción: se desinstaló la app,
                // se limpiaron los datos del sitio, etc. Se descarta sola.
                vencidas.Add(suscripcion);
            }
            catch (Exception ex)
            {
                // Un dispositivo que falla no frena el aviso a los demás.
                _logger.LogWarning(ex, "No se pudo enviar la notificación push a un dispositivo.");
            }
        }

        if (vencidas.Count > 0)
        {
            _context.SuscripcionesPush.RemoveRange(vencidas);
            await _context.SaveChangesAsync(ct);
        }
    }
}
