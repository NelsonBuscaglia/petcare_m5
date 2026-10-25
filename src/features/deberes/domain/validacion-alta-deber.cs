using PetCare.Shared.Resultados;
using PetCare.Shared.Tiempo;

namespace PetCare.Features.Deberes.Domain;

public sealed record AltaDeberValida(TipoDeber Tipo, TimeOnly Hora, Repeticion Repeticion);

public static class ValidacionAltaDeber
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
        else if (FormatoHora.TryParse(hora, out var horaLeida))
        {
            horaValida = horaLeida;
        }
        else
        {
            errores.Add(AltaDeberError.HoraInvalida);
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
}
