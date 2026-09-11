using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Api.Entities;

/// <summary>
/// Subdivisión de la categoría Productos: cremas, shampoo, tinturas.
/// Los trabajos que no son productos la dejan en null.
/// </summary>
public class Subcategoria : EntidadAuditable
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Versión para URL: "cremas-de-tratamiento". Se genera sola.</summary>
    [Required, StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Descripcion { get; set; }

    /// <summary>Clase de Font Awesome, ej: "fa-solid fa-pump-soap".</summary>
    [StringLength(60)]
    public string? Icono { get; set; }

    public int Orden { get; set; }

    public bool Activa { get; set; } = true;

    public ICollection<Trabajo> Trabajos { get; set; } = new List<Trabajo>();
}