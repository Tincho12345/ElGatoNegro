using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Shared.DTOs;

public class TrabajoDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Publicado { get; set; }
    public bool Destacado { get; set; }

    public Guid CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public string CategoriaSlug { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }

    public List<MediaDto> Medios { get; set; } = new();
}

public class TrabajoCreateDto
{
    [Required(ErrorMessage = "El título es obligatorio")]
    [StringLength(160)]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Elegí una categoría")]
    public Guid CategoriaId { get; set; }

    public bool Publicado { get; set; } = true;

    public bool Destacado { get; set; }
}