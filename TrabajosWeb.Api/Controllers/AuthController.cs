using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrabajosWeb.Api.Configuration;
using TrabajosWeb.Api.Data;
using TrabajosWeb.Api.Entities;
using TrabajosWeb.Api.Services;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext context,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == dto.NombreUsuario, ct);

        // Mismo mensaje para usuario inexistente y contraseña incorrecta,
        // para no revelar qué usuarios existen.
        if (usuario is null || !usuario.Activo ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            _logger.LogWarning("Intento de login fallido para '{Usuario}'", dto.NombreUsuario);
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
        }

        return Ok(await GenerarRespuestaAsync(usuario, ct));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshRequestDto dto, CancellationToken ct)
    {
        var token = await _context.RefreshTokens
            .Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken, ct);

        if (token is null || !token.EstaActivo || token.Usuario is null || !token.Usuario.Activo)
            return Unauthorized(new { mensaje = "Refresh token inválido o expirado." });

        // Rotación: el token usado se revoca y se emite uno nuevo
        token.Revocado = DateTime.UtcNow;

        return Ok(await GenerarRespuestaAsync(token.Usuario, ct));
    }

    private async Task<TokenResponseDto> GenerarRespuestaAsync(Usuario usuario, CancellationToken ct)
    {
        var (accessToken, expira) = _tokenService.GenerarAccessToken(usuario);
        var refreshToken = _tokenService.GenerarRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UsuarioId = usuario.Id,
            Token = refreshToken,
            Expira = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        });

        await _context.SaveChangesAsync(ct);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiraEn = expira,
            NombreUsuario = usuario.NombreUsuario
        };
    }
}