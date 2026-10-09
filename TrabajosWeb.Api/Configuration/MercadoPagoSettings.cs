namespace TrabajosWeb.Api.Configuration;

public class MercadoPagoSettings
{
    public const string SectionName = "MercadoPago";

    /// <summary>Access Token de la cuenta que cobra (credenciales de producción o de prueba).</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Texto que ve la clienta en el resumen de su tarjeta. Máximo 22 caracteres.</summary>
    public string DescriptorResumen { get; set; } = "GATO NEGRO";
}
