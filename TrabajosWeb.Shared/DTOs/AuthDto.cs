using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Shared.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "El usuario es obligatorio")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = string.Empty;
}

public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
}

public class RefreshRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}