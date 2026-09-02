using System.ComponentModel.DataAnnotations;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Api.Entities;

public class Media : EntidadAuditable
{
    public Guid TrabajoId { get; set; }
    public Trabajo? Trabajo { get; set; }

    public TipoMedia Tipo { get; set; }

    [Required, StringLength(400)]
    public string RutaRelativa { get; set; } = string.Empty;

    [StringLength(200)]
    public string? TextoAlternativo { get; set; }

    public int Orden { get; set; }
}