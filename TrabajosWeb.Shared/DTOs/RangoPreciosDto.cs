namespace TrabajosWeb.Shared.DTOs;

/// <summary>
/// Precio más bajo y más alto entre los productos publicados.
/// Los dos en cero significa que todavía no hay ninguno con precio.
/// </summary>
public class RangoPreciosDto
{
    public decimal Minimo { get; set; }
    public decimal Maximo { get; set; }

    public bool HayPrecios => Maximo > 0;
}