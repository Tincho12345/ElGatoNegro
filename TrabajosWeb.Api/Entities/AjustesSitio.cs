using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Api.Entities;

/// <summary>
/// Datos editables del sitio. Es una sola fila: no se crea ni se borra,
/// solo se edita desde el panel.
/// </summary>
public class AjustesSitio : EntidadAuditable
{
    // ---------- Identidad ----------

    [Required, StringLength(80)]
    public string NombreSitio { get; set; } = "El Gato Negro";

    /// <summary>Frase corta arriba del nombre en la portada.</summary>
    [StringLength(80)]
    public string? Saludo { get; set; }

    /// <summary>Texto de presentación de la portada y del pie.</summary>
    [StringLength(500)]
    public string? TextoBienvenida { get; set; }

    // ---------- Contacto ----------

    /// <summary>Solo números, con código de país: 5493751591536</summary>
    [StringLength(20)]
    public string? WhatsAppNumero { get; set; }

    /// <summary>Texto con el que se abre el chat de WhatsApp.</summary>
    [StringLength(300)]
    public string? MensajeWhatsApp { get; set; }

    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(40)]
    public string? Telefono { get; set; }

    [StringLength(200)]
    public string? Direccion { get; set; }

    /// <summary>Horarios de atención, en texto libre.</summary>
    [StringLength(300)]
    public string? Horarios { get; set; }

    // ---------- Redes ----------

    /// <summary>URL completa del perfil. Vacío: no se muestra el botón.</summary>
    [StringLength(300)]
    public string? Facebook { get; set; }

    [StringLength(300)]
    public string? Instagram { get; set; }

    [StringLength(300)]
    public string? TikTok { get; set; }
}