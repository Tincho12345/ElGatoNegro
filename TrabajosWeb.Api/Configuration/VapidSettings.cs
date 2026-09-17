namespace TrabajosWeb.Api.Configuration;

public class VapidSettings
{
    public const string SectionName = "Vapid";

    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>Un "mailto:" o "https:" que identifica quién manda las notificaciones.</summary>
    public string Subject { get; set; } = string.Empty;
}
