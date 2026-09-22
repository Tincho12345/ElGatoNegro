using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Shared.DTOs;

public class PedidoDto
{
    public Guid Id { get; set; }
    public int Numero { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Nota { get; set; }
    public decimal Total { get; set; }
    public bool Leido { get; set; }

    public DateTime CreatedDate { get; set; }

    public List<PedidoItemDto> Items { get; set; } = new();
}

public class PedidoItemDto
{
    public Guid Id { get; set; }
    public Guid? TrabajoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Lo que manda el carrito del sitio al confirmar. Nunca trae precios:
/// el precio siempre se recalcula en el servidor a partir del Trabajo actual.</summary>
public class PedidoCreateDto
{
    [Required(ErrorMessage = "Necesitamos tu nombre")]
    [StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Necesitamos un teléfono para responderte")]
    [StringLength(40)]
    public string Telefono { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Nota { get; set; }

    public List<PedidoItemCreateDto> Items { get; set; } = new();
}

public class PedidoItemCreateDto
{
    [Required]
    public Guid TrabajoId { get; set; }

    [Range(1, 99)]
    public int Cantidad { get; set; } = 1;
}
