using TrabajosWeb.Shared.DTOs;
using TrabajosWeb.Web.Configuration;

namespace TrabajosWeb.Web.Models;

public class GaleriaViewModel
{
    public List<TrabajoDto> Trabajos { get; set; } = new();
    public List<CategoriaDto> Categorias { get; set; } = new();
    public List<CategoriaDto> Servicios { get; set; } = new();
    public string? CategoriaActual { get; set; }
    public ContactoSettings Contacto { get; set; } = new();
    public string ApiBaseUrl { get; set; } = string.Empty;
}

public class ContactoViewModel
{
    public ConsultaCreateDto Consulta { get; set; } = new();
    public ContactoSettings Contacto { get; set; } = new();
}