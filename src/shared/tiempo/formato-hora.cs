using System.Globalization;
using System.Text.RegularExpressions;

namespace PetCare.Shared.Tiempo;

/// <summary>Hora tal como llega de un formulario: <c>HH:MM</c> en 24 h, exactamente 5 caracteres.</summary>
public static partial class FormatoHora
{
    public static bool TryParse(string? texto, out TimeOnly hora)
    {
        if (texto is null || !Patron().IsMatch(texto))
        {
            hora = default;
            return false;
        }

        hora = TimeOnly.ParseExact(texto, "HH:mm", CultureInfo.InvariantCulture);
        return true;
    }

    // `\z` y no `$`: `$` aceptaría un salto de línea final ("18:00\n").
    [GeneratedRegex(@"^([01][0-9]|2[0-3]):[0-5][0-9]\z", RegexOptions.CultureInvariant)]
    private static partial Regex Patron();
}
