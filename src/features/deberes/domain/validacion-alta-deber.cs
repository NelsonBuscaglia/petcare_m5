using System.Globalization;
using System.Text.RegularExpressions;
using PetCare.Shared.Resultados;

namespace PetCare.Features.Deberes.Domain;

public sealed record AltaDeberValida(TipoDeber Tipo, TimeOnly Hora, Repeticion Repeticion);

public static partial class ValidacionAltaDeber
{
    /// <summary>
    /// Valida los tres campos del alta tal como llegan del formulario y devuelve todos los errores
    /// encontrados, en el orden tipo, hora, repetición.
    /// </summary>
    public static Resultado<AltaDeberValida, AltaDeberError> Validar(string? tipo, string? hora, string? repeticion)
    {
        var errores = new List<AltaDeberError>();

        TipoDeber? tipoValido = tipo switch
        {
            "comida" => TipoDeber.Comida,
            "paseo" => TipoDeber.Paseo,
            _ => null,
        };
        if (tipoValido is null)
        {
            errores.Add(AltaDeberError.TipoInvalido);
        }

        TimeOnly? horaValida = null;
        if (string.IsNullOrWhiteSpace(hora))
        {
            errores.Add(AltaDeberError.HoraObligatoria);
        }
        else if (!FormatoHora().IsMatch(hora))
        {
            errores.Add(AltaDeberError.HoraInvalida);
        }
        else
        {
            horaValida = TimeOnly.ParseExact(hora, "HH:mm", CultureInfo.InvariantCulture);
        }

        Repeticion? repeticionValida = repeticion switch
        {
            "solo-hoy" => Repeticion.SoloHoy,
            "todos-los-dias" => Repeticion.TodosLosDias,
            _ => null,
        };
        if (repeticionValida is null)
        {
            errores.Add(AltaDeberError.RepeticionObligatoria);
        }

        return errores.Count > 0
            ? Resultado<AltaDeberValida, AltaDeberError>.Falla(errores)
            : Resultado<AltaDeberValida, AltaDeberError>.Ok(
                new AltaDeberValida(tipoValido!.Value, horaValida!.Value, repeticionValida!.Value));
    }

    // `\z` y no `$`: `$` aceptaría un salto de línea final ("18:00\n").
    [GeneratedRegex(@"^([01][0-9]|2[0-3]):[0-5][0-9]\z", RegexOptions.CultureInvariant)]
    private static partial Regex FormatoHora();
}
