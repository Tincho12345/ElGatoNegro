using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Api.Entities;

public class Usuario : EntidadAuditable
{
    [Required, StringLength(80)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(400)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Rol { get; set; } = "Admin";

    public bool Activo { get; set; } = true;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public class RefreshToken : EntidadAuditable
{
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    [Required, StringLength(200)]
    public string Token { get; set; } = string.Empty;

    public DateTime Expira { get; set; }
    public DateTime? Revocado { get; set; }

    public bool EstaActivo => Revocado is null && DateTime.UtcNow < Expira;
}