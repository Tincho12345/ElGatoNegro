using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Api.Entities;

public class Trabajo : EntidadAuditable
{
    [Required, StringLength(160)]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Descripcion { get; set; }

    public bool Publicado { get; set; } = true;

    /// <summary>Destacado: aparece primero en la galería.</summary>
    public bool Destacado { get; set; }

    public Guid CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public ICollection<Media> Medios { get; set; } = new List<Media>();
}