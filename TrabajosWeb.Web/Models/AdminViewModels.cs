using Microsoft.AspNetCore.Mvc.Rendering;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Web.Models;

public class PanelViewModel
{
    public List<TrabajoDto> Trabajos { get; set; } = new();
    public List<CategoriaDto> Categorias { get; set; } = new();
    public int ConsultasSinLeer { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
}

public class TrabajoFormViewModel
{
    public Guid? Id { get; set; }
    public TrabajoCreateDto Trabajo { get; set; } = new();
    public List<MediaDto> MediosActuales { get; set; } = new();
    public List<SelectListItem> Categorias { get; set; } = new();
    public List<SelectListItem> Subcategorias { get; set; } = new();
    public List<SelectListItem> Marcas { get; set; } = new();
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Id de la categoría Productos. La vista lo usa para mostrar u ocultar
    /// los campos de subcategoría y marca según lo que se elija.
    /// </summary>
    public Guid? CategoriaProductosId { get; set; }

    public bool EsNuevo => Id is null;
}

public class CategoriaFormViewModel
{
    public Guid? Id { get; set; }
    public CategoriaCreateDto Categoria { get; set; } = new();

    public bool EsNuevo => Id is null;
}

public class SubcategoriaFormViewModel
{
    public Guid? Id { get; set; }
    public SubcategoriaCreateDto Subcategoria { get; set; } = new();

    public bool EsNuevo => Id is null;
}

public class MarcaFormViewModel
{
    public Guid? Id { get; set; }
    public MarcaCreateDto Marca { get; set; } = new();

    public bool EsNuevo => Id is null;
}

public class ConsultasViewModel
{
    public List<ConsultaDto> Consultas { get; set; } = new();
    public bool SoloNoLeidas { get; set; }
}