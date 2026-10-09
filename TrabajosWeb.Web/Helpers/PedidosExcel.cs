using ClosedXML.Excel;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Web.Helpers;

public static class PedidosExcel
{
    private const string FormatoImporte = "\"$\" #,##0";
    private const string FormatoFecha = "dd/MM/yyyy HH:mm";

    /// <summary>Dos hojas: una fila por pedido y una fila por producto de cada pedido.</summary>
    public static byte[] Generar(IReadOnlyList<PedidoDto> pedidos)
    {
        using var libro = new XLWorkbook();

        ArmarHojaPedidos(libro.Worksheets.Add("Pedidos"), pedidos);
        ArmarHojaDetalle(libro.Worksheets.Add("Detalle"), pedidos);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void ArmarHojaPedidos(IXLWorksheet hoja, IReadOnlyList<PedidoDto> pedidos)
    {
        string[] titulos = { "N°", "Fecha", "Nombre", "Teléfono", "Pago", "Fecha de pago", "Leído", "Total", "Nota" };
        PonerTitulos(hoja, titulos);

        var fila = 2;
        foreach (var p in pedidos)
        {
            hoja.Cell(fila, 1).Value = p.Numero;
            hoja.Cell(fila, 2).Value = p.CreatedDate.ToLocalTime();
            hoja.Cell(fila, 3).Value = p.Nombre;
            hoja.Cell(fila, 4).Value = p.Telefono ?? string.Empty;
            hoja.Cell(fila, 5).Value = TextoPago(p);

            if (p.PagadoEl.HasValue)
                hoja.Cell(fila, 6).Value = p.PagadoEl.Value.ToLocalTime();

            hoja.Cell(fila, 7).Value = p.Leido ? "Sí" : "No";
            hoja.Cell(fila, 8).Value = p.Total;
            hoja.Cell(fila, 9).Value = p.Nota ?? string.Empty;
            fila++;
        }

        hoja.Column(2).Style.DateFormat.Format = FormatoFecha;
        hoja.Column(4).Style.NumberFormat.Format = "@";
        hoja.Column(6).Style.DateFormat.Format = FormatoFecha;
        hoja.Column(8).Style.NumberFormat.Format = FormatoImporte;

        Terminar(hoja, titulos.Length);
    }

    private static void ArmarHojaDetalle(IXLWorksheet hoja, IReadOnlyList<PedidoDto> pedidos)
    {
        string[] titulos = { "N° pedido", "Fecha", "Nombre", "Pago", "Producto", "Cantidad", "Precio", "Subtotal" };
        PonerTitulos(hoja, titulos);

        var fila = 2;
        foreach (var p in pedidos)
        {
            foreach (var item in p.Items)
            {
                hoja.Cell(fila, 1).Value = p.Numero;
                hoja.Cell(fila, 2).Value = p.CreatedDate.ToLocalTime();
                hoja.Cell(fila, 3).Value = p.Nombre;
                hoja.Cell(fila, 4).Value = TextoPago(p);
                hoja.Cell(fila, 5).Value = item.Titulo;
                hoja.Cell(fila, 6).Value = item.Cantidad;
                hoja.Cell(fila, 7).Value = item.Precio;
                hoja.Cell(fila, 8).Value = item.Subtotal;
                fila++;
            }
        }

        hoja.Column(2).Style.DateFormat.Format = FormatoFecha;
        hoja.Column(7).Style.NumberFormat.Format = FormatoImporte;
        hoja.Column(8).Style.NumberFormat.Format = FormatoImporte;

        Terminar(hoja, titulos.Length);
    }

    private static void PonerTitulos(IXLWorksheet hoja, string[] titulos)
    {
        for (var i = 0; i < titulos.Length; i++)
            hoja.Cell(1, i + 1).Value = titulos[i];

        var encabezado = hoja.Range(1, 1, 1, titulos.Length);
        encabezado.Style.Font.Bold = true;
        encabezado.Style.Fill.BackgroundColor = XLColor.LightGray;
    }

    private static void Terminar(IXLWorksheet hoja, int columnas)
    {
        hoja.Range(1, 1, Math.Max(hoja.LastRowUsed()?.RowNumber() ?? 1, 1), columnas).SetAutoFilter();
        hoja.SheetView.FreezeRows(1);
        hoja.Columns(1, columnas).AdjustToContents(1, 200, 10, 60);
    }

    private static string TextoPago(PedidoDto p) => p.EstadoPago switch
    {
        EstadoPago.Aprobado => "Pagado (Mercado Pago)",
        EstadoPago.Pendiente => "Pendiente (Mercado Pago)",
        EstadoPago.Rechazado => "Rechazado (Mercado Pago)",
        EstadoPago.Reembolsado => "Reembolsado (Mercado Pago)",
        _ => "A coordinar"
    };
}
