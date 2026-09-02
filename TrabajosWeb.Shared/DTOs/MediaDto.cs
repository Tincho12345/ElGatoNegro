namespace TrabajosWeb.Shared.DTOs;

public enum TipoMedia
{
    Imagen = 1,
    Video = 2
}

public class MediaDto
{
    public Guid Id { get; set; }
    public Guid TrabajoId { get; set; }
    public TipoMedia Tipo { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? TextoAlternativo { get; set; }
    public int Orden { get; set; }
}