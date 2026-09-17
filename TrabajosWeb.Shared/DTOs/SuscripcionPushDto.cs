using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Shared.DTOs;

public class SuscripcionPushCreateDto
{
    [Required, StringLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string P256dh { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Auth { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Dispositivo { get; set; }
}

public class SuscripcionPushEliminarDto
{
    [Required, StringLength(500)]
    public string Endpoint { get; set; } = string.Empty;
}
