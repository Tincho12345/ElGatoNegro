using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TrabajosWeb.Shared.Helpers;

public static class SlugHelper
{
    /// <summary>
    /// Convierte "Brushing y Peinados" en "brushing-y-peinados".
    /// Quita acentos y todo lo que no sea letra, número o guion.
    /// </summary>
    public static string Generar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalizado)
        {
            // La ñ se conserva; el resto de los diacríticos se descarta
            if (c == 'ñ' || c == 'Ñ')
            {
                sb.Append('n');
                continue;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var limpio = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        limpio = Regex.Replace(limpio, @"[^a-z0-9\s-]", "");
        limpio = Regex.Replace(limpio, @"[\s-]+", "-").Trim('-');

        return limpio;
    }
}