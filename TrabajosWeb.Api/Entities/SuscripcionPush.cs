using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Api.Entities;

/// <summary>
/// Un dispositivo suscripto a notificaciones push. Cada vez que alguien
/// acepta el permiso de notificaciones desde el panel, el navegador genera
/// un endpoint único y dos claves de cifrado; eso es lo que se guarda acá
/// para poder avisarle después, aunque tenga el navegador cerrado.
/// </summary>
public class SuscripcionPush : EntidadAuditable
{
    [Required, StringLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string P256dh { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Auth { get; set; } = string.Empty;

    /// <summary>Para reconocer el dispositivo si hace falta. Ej: "Chrome en Windows".</summary>
    [StringLength(200)]
    public string? Dispositivo { get; set; }
}
