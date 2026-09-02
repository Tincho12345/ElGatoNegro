using System.Security.Claims;

namespace TrabajosWeb.Api.Services;

public interface IUsuarioActualService
{
    string? NombreUsuario { get; }
}

public class UsuarioActualService : IUsuarioActualService
{
    private readonly IHttpContextAccessor _accessor;

    public UsuarioActualService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? NombreUsuario =>
        _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
}