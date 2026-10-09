using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TrabajosWeb.Shared.DTOs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MetodoPago
{
    /// <summary>El salón y la clienta arreglan el pago por afuera del sitio.</summary>
    Coordinar = 0,
    MercadoPago = 1
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EstadoPago
{
    /// <summary>No hay pago online: se coordina con el salón.</summary>
    SinPago = 0,
    Pendiente = 1,
    Aprobado = 2,
    Rechazado = 3,
    Reembolsado = 4
}

public class PedidoDto
{
    public Guid Id { get; set; }
    public int Numero { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Nota { get; set; }
    public decimal Total { get; set; }
    public bool Leido { get; set; }

    public MetodoPago MetodoPago { get; set; }
    public EstadoPago EstadoPago { get; set; }
    public DateTime? PagadoEl { get; set; }

    public DateTime CreatedDate { get; set; }

    public List<PedidoItemDto> Items { get; set; } = new();
}

/// <summary>
/// Filtros del listado de pedidos del panel. Los usa la API para consultar y la
/// Web para armar la URL, así la lista y el Excel filtran siempre igual.
/// </summary>
public class PedidoFiltroDto
{
    /// <summary>Busca en número de pedido, nombre, teléfono, nota y productos.</summary>
    public string? Texto { get; set; }

    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }

    /// <summary>true: solo leídos. false: solo sin leer. null: todos.</summary>
    public bool? Leido { get; set; }

    public EstadoPago? EstadoPago { get; set; }

    public int Pagina { get; set; } = 1;

    /// <summary>Pedidos por página. 0 trae todos (lo usa la exportación a Excel).</summary>
    public int Tamano { get; set; } = 20;

    public bool TieneFiltros =>
        !string.IsNullOrWhiteSpace(Texto) || Desde.HasValue || Hasta.HasValue
        || Leido.HasValue || EstadoPago.HasValue;

    /// <summary>Parámetros de URL con los filtros activos, sin signo "?" inicial.</summary>
    public string ToQueryString(bool conPaginado = true)
    {
        var partes = new List<string>();

        if (!string.IsNullOrWhiteSpace(Texto))
            partes.Add($"texto={Uri.EscapeDataString(Texto.Trim())}");
        if (Desde.HasValue)
            partes.Add($"desde={Desde.Value:yyyy-MM-dd}");
        if (Hasta.HasValue)
            partes.Add($"hasta={Hasta.Value:yyyy-MM-dd}");
        if (Leido.HasValue)
            partes.Add($"leido={(Leido.Value ? "true" : "false")}");
        if (EstadoPago.HasValue)
            partes.Add($"estadoPago={EstadoPago.Value}");
        if (conPaginado)
        {
            partes.Add($"pagina={Pagina}");
            partes.Add($"tamano={Tamano}");
        }

        return string.Join("&", partes);
    }
}

public class PedidosPaginaDto
{
    public List<PedidoDto> Items { get; set; } = new();

    /// <summary>Cantidad de pedidos que cumplen los filtros, sumando todas las páginas.</summary>
    public int Total { get; set; }

    /// <summary>Suma del importe de todos los pedidos que cumplen los filtros.</summary>
    public decimal ImporteTotal { get; set; }

    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
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

    public MetodoPago MetodoPago { get; set; } = MetodoPago.Coordinar;

    public List<PedidoItemCreateDto> Items { get; set; } = new();
}

public class PedidoItemCreateDto
{
    [Required]
    public Guid TrabajoId { get; set; }

    [Range(1, 99)]
    public int Cantidad { get; set; } = 1;
}

/// <summary>Lo arma el sitio (no el navegador) para pedirle a la API el link de pago.</summary>
public class PreferenciaPagoRequestDto
{
    /// <summary>Página del sitio a la que vuelve la clienta desde Mercado Pago.</summary>
    [Required]
    public string UrlRetorno { get; set; } = string.Empty;

    /// <summary>Dirección pública donde Mercado Pago avisa del pago. Null en desarrollo local.</summary>
    public string? UrlNotificacion { get; set; }
}

public class PreferenciaPagoDto
{
    public string UrlPago { get; set; } = string.Empty;
}

public class OpcionesPagoDto
{
    public bool MercadoPago { get; set; }
}

public class SincronizarPagoDto
{
    [Required]
    public string PaymentId { get; set; } = string.Empty;
}

public class EstadoPedidoDto
{
    public Guid PedidoId { get; set; }
    public int Numero { get; set; }
    public EstadoPago EstadoPago { get; set; }
}
