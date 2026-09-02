using System.Security.Claims;
using TrabajosWeb.Api.Entities;

namespace TrabajosWeb.Api.Services;

public interface ITokenService
{
    (string Token, DateTime Expira) GenerarAccessToken(Usuario usuario);
    string GenerarRefreshToken();
}