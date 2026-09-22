namespace TrabajosWeb.Web.Helpers;

/// <summary>
/// La API devolvió 401: el token guardado en la cookie ya no sirve.
///
/// Existe para que las pantallas no confundan "no hay datos" con "no hay
/// sesión". Sin esto, un token vencido hace que ApiClient devuelva null o
/// una lista vacía y cada vista lo interpreta a su manera: paneles con
/// todo en cero, como si no hubiera nada cargado en vez de avisar que hay
/// que volver a entrar.
/// </summary>
public class SesionExpiradaException(string mensaje = "Tu sesión expiró. Volvé a iniciar sesión para continuar.")
    : Exception(mensaje);
