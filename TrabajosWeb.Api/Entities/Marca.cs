using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Api.Entities;

/// <summary>
/// Marca de un producto. Los trabajos que no son productos la dejan en null.
/// </summary>
public class Marca : EntidadAuditable
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Versión para URL: "loreal-professionnel". Se genera sola.</summary>
    [Required, StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    public int Orden { get; set; }

    public bool Activa { get; set; } = true;

    public ICollection<Trabajo> Trabajos { get; set; } = new List<Trabajo>();
}