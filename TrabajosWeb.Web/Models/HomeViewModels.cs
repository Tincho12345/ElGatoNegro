using TrabajosWeb.Shared.DTOs;
using TrabajosWeb.Web.Configuration;

namespace TrabajosWeb.Web.Models;

public class GaleriaViewModel
{
    public List<TrabajoDto> Trabajos { get; set; } = new();
    public List<CategoriaDto> Categorias { get; set; } = new();
    public List<CategoriaDto> Servicios { get; set; } = new();

    /// <summary>Otros trabajos de la misma categoría, para la vista de detalle.</summary>
    public List<TrabajoDto> Relacionados { get; set; } = new();

    public string? CategoriaActual { get; set; }
    public ContactoSettings Contacto { get; set; } = new();
    public string ApiBaseUrl { get; set; } = string.Empty;
}

public class ContactoViewModel
{
    public ConsultaCreateDto Consulta { get; set; } = new();
    public ContactoSettings Contacto { get; set; } = new();

    /// <summary>
    /// Cuando se llega desde el detalle de un trabajo, se muestra de qué
    /// se trata la consulta y el mensaje viene escrito.
    /// </summary>
    public TrabajoDto? Trabajo { get; set; }

    public string ApiBaseUrl { get; set; } = string.Empty;
}
