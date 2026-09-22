namespace TrabajosWeb.Api.Services;

public interface IPushNotificationService
{
    /// <summary>
    /// Le manda el aviso a todos los dispositivos suscriptos. Nunca lanza
    /// una excepción hacia afuera: un fallo al notificar no puede tumbar
    /// la operación que lo disparó (por ejemplo, guardar una consulta nueva).
    /// </summary>
    /// <param name="consultaId">
    /// Si se pasa, el aviso incluye el botón "Marcar como leída" y viaja
    /// con este Id en el payload para que el service worker sepa cuál
    /// consulta marcar sin tener que abrir el panel.
    /// </param>
    Task NotificarATodosAsync(
        string titulo,
        string cuerpo,
        string url,
        Guid? consultaId = null,
        CancellationToken ct = default);
}
