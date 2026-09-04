using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Shared.DTOs;

public class CategoriaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Icono { get; set; }
    public string? TextoServicio { get; set; }
    public bool MostrarEnServicios { get; set; }
    public int Orden { get; set; }
    public bool Activa { get; set; }
    public int CantidadTrabajos { get; set; }
}

public class CategoriaCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Descripcion { get; set; }

    [StringLength(60)]
    public string? Icono { get; set; }

    [StringLength(500)]
    public string? TextoServicio { get; set; }

    public bool MostrarEnServicios { get; set; } = true;

    public int Orden { get; set; }

    public bool Activa { get; set; } = true;
}