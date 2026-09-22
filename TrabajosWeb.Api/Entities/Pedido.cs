using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrabajosWeb.Api.Entities;

public class Pedido : EntidadAuditable
{
    /// <summary>Número correlativo para mostrarle a la clienta y al panel. No es el Id.</summary>
    public int Numero { get; set; }

    [Required, StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Telefono { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Nota { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Total { get; set; }

    public bool Leido { get; set; }

    public ICollection<PedidoItem> Items { get; set; } = new List<PedidoItem>();
}
