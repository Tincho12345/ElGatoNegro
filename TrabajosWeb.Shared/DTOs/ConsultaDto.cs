using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Shared.DTOs;

public class ConsultaCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Teléfono inválido")]
    [StringLength(40)]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El mensaje es obligatorio")]
    [StringLength(2000)]
    public string Mensaje { get; set; } = string.Empty;
}

public class ConsultaDto : ConsultaCreateDto
{
    public Guid Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool Leida { get; set; }
}