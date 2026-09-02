using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Api.Entities;

public class Consulta : EntidadAuditable
{
    [Required, StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Telefono { get; set; }

    [Required, StringLength(2000)]
    public string Mensaje { get; set; } = string.Empty;

    public bool Leida { get; set; }
}