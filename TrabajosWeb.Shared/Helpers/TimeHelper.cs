namespace TrabajosWeb.Shared.Helpers;

/// <summary>
/// Centraliza el manejo de fechas en horario argentino (UTC-3, sin DST).
/// Todo se persiste en UTC; la conversión a local es solo para mostrar.
/// </summary>
public static class TimeHelper
{
    private static readonly TimeZoneInfo ZonaArgentina = ObtenerZona();

    private static TimeZoneInfo ObtenerZona()
    {
        // El id cambia según el SO: Windows usa uno, Linux/Docker el de la IANA.
        foreach (var id in new[] { "Argentina Standard Time", "America/Argentina/Buenos_Aires" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        // Último recurso: offset fijo. Argentina no aplica horario de verano.
        return TimeZoneInfo.CreateCustomTimeZone("ART", TimeSpan.FromHours(-3), "Hora Argentina", "ART");
    }

    /// <summary>Momento actual en horario argentino.</summary>
    public static DateTime Ahora =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaArgentina);

    /// <summary>Momento actual en UTC. Es lo que se guarda en la base.</summary>
    public static DateTime AhoraUtc => DateTime.UtcNow;

    /// <summary>Fecha de hoy en horario argentino, sin hora.</summary>
    public static DateTime Hoy => Ahora.Date;

    /// <summary>Convierte un valor UTC de la base a horario argentino.</summary>
    public static DateTime AHoraLocal(DateTime utc)
    {
        var origen = utc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            : utc.ToUniversalTime();

        return TimeZoneInfo.ConvertTimeFromUtc(origen, ZonaArgentina);
    }

    /// <summary>Igual que la anterior, tolerando nulos.</summary>
    public static DateTime? AHoraLocal(DateTime? utc) =>
        utc.HasValue ? AHoraLocal(utc.Value) : null;

    /// <summary>Convierte una hora local argentina a UTC para persistirla.</summary>
    public static DateTime AUtc(DateTime local)
    {
        var origen = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(origen, ZonaArgentina);
    }

    /// <summary>Formato corto para mostrar: 01/09/2026 10:22</summary>
    public static string Formatear(DateTime utc) =>
        AHoraLocal(utc).ToString("dd/MM/yyyy HH:mm");

    public static string Formatear(DateTime? utc) =>
        utc.HasValue ? Formatear(utc.Value) : string.Empty;
}