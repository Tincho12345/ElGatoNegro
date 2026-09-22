using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrabajosWeb.Api.Entities;

/// <summary>
/// Línea de un pedido. Guarda su propia foto de Título/Precio en el momento
/// de la compra: si el trabajo cambia de precio o se borra más adelante, el
/// pedido histórico no se ve afectado.
/// </summary>
public class PedidoItem : EntidadAuditable
{
    public Guid PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    /// <summary>Nullable a propósito: sobrevive aunque el trabajo se borre.</summary>
    public Guid? TrabajoId { get; set; }
    public Trabajo? Trabajo { get; set; }

    [Required, StringLength(160)]
    public string Titulo { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12,2)")]
    public decimal Precio { get; set; }

    public int Cantidad { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Subtotal { get; set; }
}
