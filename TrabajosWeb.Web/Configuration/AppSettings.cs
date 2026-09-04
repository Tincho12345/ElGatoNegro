namespace TrabajosWeb.Web.Configuration;

public class ApiSettings
{
    public const string SectionName = "ApiSettings";
    public string BaseUrl { get; set; } = string.Empty;
}

public class ContactoSettings
{
    public const string SectionName = "Contacto";

    public string WhatsAppNumero { get; set; } = string.Empty;
    public string MensajePredeterminado { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>URL completa del perfil, ej: "https://facebook.com/gatonegro". Vacío = no se muestra.</summary>
    public string Facebook { get; set; } = string.Empty;

    /// <summary>URL completa del perfil, ej: "https://instagram.com/gatonegro". Vacío = no se muestra.</summary>
    public string Instagram { get; set; } = string.Empty;

    /// <summary>Link listo para usar en los botones de WhatsApp.</summary>
    public string LinkWhatsApp =>
        $"https://wa.me/{WhatsAppNumero}?text={Uri.EscapeDataString(MensajePredeterminado)}";

    public bool TieneRedes =>
        !string.IsNullOrWhiteSpace(Facebook) || !string.IsNullOrWhiteSpace(Instagram);
}