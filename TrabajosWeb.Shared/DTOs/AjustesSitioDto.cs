using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Shared.DTOs;

public class AjustesSitioDto
{
    public Guid Id { get; set; }

    public string NombreSitio { get; set; } = string.Empty;
    public string? Saludo { get; set; }
    public string? TextoBienvenida { get; set; }

    public string? WhatsAppNumero { get; set; }
    public string? MensajeWhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Horarios { get; set; }

    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? TikTok { get; set; }

    /// <summary>Link listo para los botones de WhatsApp.</summary>
    public string LinkWhatsApp =>
        string.IsNullOrWhiteSpace(WhatsAppNumero)
            ? string.Empty
            : $"https://wa.me/{WhatsAppNumero}?text={Uri.EscapeDataString(MensajeWhatsApp ?? string.Empty)}";

    public bool TieneWhatsApp => !string.IsNullOrWhiteSpace(WhatsAppNumero);

    public bool TieneRedes =>
        !string.IsNullOrWhiteSpace(Facebook)
        || !string.IsNullOrWhiteSpace(Instagram)
        || !string.IsNullOrWhiteSpace(TikTok);
}

public class AjustesSitioUpdateDto
{
    [Required(ErrorMessage = "El nombre del sitio es obligatorio")]
    [StringLength(80)]
    public string NombreSitio { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Saludo { get; set; }

    [StringLength(500)]
    public string? TextoBienvenida { get; set; }

    [StringLength(20)]
    [RegularExpression(@"^\d*$", ErrorMessage = "Solo números, sin espacios ni signos")]
    public string? WhatsAppNumero { get; set; }

    [StringLength(300)]
    public string? MensajeWhatsApp { get; set; }

    [EmailAddress(ErrorMessage = "Email inválido")]
    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(40)]
    public string? Telefono { get; set; }

    [StringLength(200)]
    public string? Direccion { get; set; }

    [StringLength(300)]
    public string? Horarios { get; set; }

    [Url(ErrorMessage = "Tiene que ser una dirección completa, con https://")]
    [StringLength(300)]
    public string? Facebook { get; set; }

    [Url(ErrorMessage = "Tiene que ser una dirección completa, con https://")]
    [StringLength(300)]
    public string? Instagram { get; set; }

    [Url(ErrorMessage = "Tiene que ser una dirección completa, con https://")]
    [StringLength(300)]
    public string? TikTok { get; set; }
}