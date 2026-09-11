using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    /// <summary>Precio actual. Opcional: no todos los trabajos publican precio.</summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal? Precio { get; set; }

    /// <summary>Precio anterior, solo para mostrarlo tachado al lado del actual.</summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal? PrecioAnterior { get; set; }

    /// <summary>Cartelito corto sobre la foto: "35% OFF", "2x1". Si se deja vacío y hay
    /// precio anterior, el porcentaje se calcula solo.</summary>
    [StringLength(40)]
    public string? EtiquetaOferta { get; set; }

    /// <summary>Línea libre: "Pague dos y lleve tres".</summary>
    [StringLength(200)]
    public string? TextoOferta { get; set; }

    public Guid CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    /// <summary>Solo la usan los productos; el resto de los trabajos va en null.</summary>
    public Guid? SubcategoriaId { get; set; }
    public Subcategoria? Subcategoria { get; set; }

    /// <summary>Solo la usan los productos; el resto de los trabajos va en null.</summary>
    public Guid? MarcaId { get; set; }
    public Marca? Marca { get; set; }

    public ICollection<Media> Medios { get; set; } = new List<Media>();
}