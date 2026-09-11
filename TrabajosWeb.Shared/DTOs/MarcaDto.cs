using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Shared.DTOs;

public class MarcaDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activa { get; set; }
    public int CantidadProductos { get; set; }
}

public class MarcaCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    public int Orden { get; set; }

    public bool Activa { get; set; } = true;
}