using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Shared.DTOs;

public class TrabajoDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Publicado { get; set; }
    public bool Destacado { get; set; }

    public decimal? Precio { get; set; }
    public decimal? PrecioAnterior { get; set; }
    public string? EtiquetaOferta { get; set; }
    public string? TextoOferta { get; set; }

    public Guid CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public string CategoriaSlug { get; set; } = string.Empty;

    public Guid? SubcategoriaId { get; set; }
    public string? SubcategoriaNombre { get; set; }
    public string? SubcategoriaSlug { get; set; }

    public Guid? MarcaId { get; set; }
    public string? MarcaNombre { get; set; }
    public string? MarcaSlug { get; set; }

    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }

    public List<MediaDto> Medios { get; set; } = new();

    /// <summary>Hay algo de oferta para mostrar: precio tachado, etiqueta o texto.</summary>
    public bool TieneOferta =>
        PrecioAnterior.HasValue
        || !string.IsNullOrWhiteSpace(EtiquetaOferta)
        || !string.IsNullOrWhiteSpace(TextoOferta);

    /// <summary>
    /// La etiqueta a mostrar. Si no se cargó una a mano pero hay precio anterior
    /// y actual, se calcula el porcentaje de descuento.
    /// </summary>
    public string? EtiquetaCalculada
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(EtiquetaOferta))
                return EtiquetaOferta;

            if (PrecioAnterior is > 0 && Precio is > 0 && PrecioAnterior > Precio)
            {
                var descuento = (int)Math.Round(
                    (PrecioAnterior.Value - Precio.Value) / PrecioAnterior.Value * 100);

                return descuento > 0 ? $"{descuento}% OFF" : null;
            }

            return null;
        }
    }
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

    /// <summary>Solo se completa cuando la categoría es Productos.</summary>
    public Guid? SubcategoriaId { get; set; }

    /// <summary>Solo se completa cuando la categoría es Productos.</summary>
    public Guid? MarcaId { get; set; }

    [Range(0, 99999999, ErrorMessage = "El precio no puede ser negativo")]
    public decimal? Precio { get; set; }

    [Range(0, 99999999, ErrorMessage = "El precio anterior no puede ser negativo")]
    public decimal? PrecioAnterior { get; set; }

    [StringLength(40)]
    public string? EtiquetaOferta { get; set; }

    [StringLength(200)]
    public string? TextoOferta { get; set; }

    public bool Publicado { get; set; } = true;

    public bool Destacado { get; set; }
}