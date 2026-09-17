namespace TrabajosWeb.Api.Services;

public interface IPushNotificationService
{
    /// <summary>
    /// Le manda el aviso a todos los dispositivos suscriptos. Nunca lanza
    /// una excepción hacia afuera: un fallo al notificar no puede tumbar
    /// la operación que lo disparó (por ejemplo, guardar una consulta nueva).
    /// </summary>
    Task NotificarATodosAsync(string titulo, string cuerpo, string url, CancellationToken ct = default);
}
