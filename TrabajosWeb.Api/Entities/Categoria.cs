using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Api.Entities;

public class Categoria : EntidadAuditable
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Versión para URL: "brushing-y-peinados". Se genera sola.</summary>
    [Required, StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Descripcion { get; set; }

    /// <summary>Clase de Font Awesome, ej: "fa-solid fa-scissors".</summary>
    [StringLength(60)]
    public string? Icono { get; set; }

    /// <summary>Texto que se muestra en la tarjeta de la sección Servicios de la home.</summary>
    [StringLength(500)]
    public string? TextoServicio { get; set; }

    /// <summary>Si aparece como servicio ofrecido en la home, más allá de la galería.</summary>
    public bool MostrarEnServicios { get; set; } = true;

    public int Orden { get; set; }

    public bool Activa { get; set; } = true;

    public ICollection<Trabajo> Trabajos { get; set; } = new List<Trabajo>();
}