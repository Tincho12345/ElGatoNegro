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
    public string ApiBaseUrl { get; set; } = string.Empty;

    public bool EsNuevo => Id is null;
}

public class CategoriaFormViewModel
{
    public Guid? Id { get; set; }
    public CategoriaCreateDto Categoria { get; set; } = new();

    public bool EsNuevo => Id is null;
}

public class ConsultasViewModel
{
    public List<ConsultaDto> Consultas { get; set; } = new();
    public bool SoloNoLeidas { get; set; }
}