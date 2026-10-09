using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TrabajosWeb.Shared.DTOs;

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

    public MetodoPago MetodoPago { get; set; }

    public EstadoPago EstadoPago { get; set; }

    /// <summary>Id del pago en Mercado Pago, para poder rastrearlo en su panel.</summary>
    [StringLength(40)]
    public string? MercadoPagoPaymentId { get; set; }

    public DateTime? PagadoEl { get; set; }

    public ICollection<PedidoItem> Items { get; set; } = new List<PedidoItem>();
}
