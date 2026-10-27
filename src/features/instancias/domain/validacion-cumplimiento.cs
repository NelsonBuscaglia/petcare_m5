using PetCare.Shared.Resultados;
using PetCare.Shared.Tiempo;

namespace PetCare.Features.Instancias.Domain;

public static class ValidacionCumplimiento
{
    /// <summary>
    /// Valida el registro de cumplimiento de una instancia y devuelve el primer error, en este
    /// orden: expiró (FR-07), ya cumplida (FR-06), hora obligatoria, formato de hora y hora
    /// posterior a la actual (FR-04). La hora se compara al minuto: a las 10:30:25 se acepta 10:30.
    /// </summary>
    public static Resultado<TimeOnly, CumplimientoError> Validar(
        string? horaReal, DateOnly fechaInstancia, bool tieneRegistro, DateTime ahoraLocal)
    {
        if (ErrorDeVigencia(fechaInstancia, tieneRegistro, ahoraLocal) is { } vigencia)
        {
            return Falla(vigencia);
        }

        if (string.IsNullOrWhiteSpace(horaReal))
        {
            return Falla(CumplimientoError.HoraObligatoria);
        }

        if (!FormatoHora.TryParse(horaReal, out var hora))
        {
            return Falla(CumplimientoError.HoraInvalida);
        }

        var hoy = DateOnly.FromDateTime(ahoraLocal);
        return hoy.ToDateTime(hora) > ahoraLocal
            ? Falla(CumplimientoError.HoraPosteriorALaActual)
            : Resultado<TimeOnly, CumplimientoError>.Ok(hora);
    }

    /// <summary>Por qué una instancia no admite registro (expiró o ya fue cumplida), o <c>null</c> si es vigente.</summary>
    public static CumplimientoError? ErrorDeVigencia(DateOnly fechaInstancia, bool tieneRegistro, DateTime ahoraLocal)
    {
        if (fechaInstancia < DateOnly.FromDateTime(ahoraLocal))
        {
            return CumplimientoError.InstanciaExpirada;
        }

        return tieneRegistro ? CumplimientoError.InstanciaYaCumplida : null;
    }

    private static Resultado<TimeOnly, CumplimientoError> Falla(CumplimientoError error) =>
        Resultado<TimeOnly, CumplimientoError>.Falla([error]);
}
