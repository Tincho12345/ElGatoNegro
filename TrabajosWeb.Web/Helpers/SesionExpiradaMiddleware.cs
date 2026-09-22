using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;

namespace TrabajosWeb.Web.Helpers;

/// <summary>
/// Traduce una sesión expirada a algo que el usuario entienda.
///
/// Si la petición vino por AJAX responde JSON con la marca sesionExpirada,
/// para que el navegador muestre el aviso sin recargar. Si fue una
/// navegación normal, cierra la sesión y manda al login con el mensaje.
/// </summary>
public class SesionExpiradaMiddleware(RequestDelegate siguiente, ILogger<SesionExpiradaMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await siguiente(context);
        }
        catch (SesionExpiradaException ex)
        {
            logger.LogInformation("Sesión expirada en {Ruta}", context.Request.Path);

            if (context.Response.HasStarted)
            {
                // Ya se empezó a mandar la página: no se puede redirigir ni
                // cambiar el código de estado. Se deja pasar.
                throw;
            }

            await CerrarSesionAsync(context);

            if (EsPeticionAjax(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";

                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    sesionExpirada = true,
                    mensaje = ex.Message,
                    loginUrl = "/Cuenta/Login"
                }));

                return;
            }

            var destino = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/Cuenta/Login?expirada=1&returnUrl={Uri.EscapeDataString(destino)}");
        }
    }

    private static async Task CerrarSesionAsync(HttpContext context)
    {
        try
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        catch
        {
            // Si falla, el login igual va a pedir credenciales
        }
    }

    /// <summary>
    /// Las llamadas por fetch piden JSON; una navegación del navegador
    /// pide HTML. Se mira eso para decidir cómo responder.
    /// </summary>
    private static bool EsPeticionAjax(HttpRequest request)
    {
        if (request.Headers.XRequestedWith == "XMLHttpRequest") return true;

        if (request.Headers.TryGetValue("Sec-Fetch-Mode", out var modo) && modo == "cors")
            return true;

        var accept = request.Headers.Accept.ToString();

        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            && !accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
