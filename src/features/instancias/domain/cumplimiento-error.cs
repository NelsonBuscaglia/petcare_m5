namespace PetCare.Features.Instancias.Domain;

public enum CumplimientoError
{
    InstanciaNoEncontrada,
    HoraObligatoria,
    HoraInvalida,
    HoraPosteriorALaActual,
    InstanciaYaCumplida,
    InstanciaExpirada,
    RegistroYaEliminado,
    RegistroDeOtroDia,
}

public static class CumplimientoErrores
{
    /// <summary>Mensaje para el cuidador. <c>InstanciaNoEncontrada</c> se responde con 404, sin mostrarlo.</summary>
    public static string Mensaje(CumplimientoError error) => error switch
    {
        CumplimientoError.InstanciaNoEncontrada => "La instancia no existe.",
        CumplimientoError.HoraObligatoria => "La hora es obligatoria.",
        CumplimientoError.HoraInvalida => "La hora no es válida: usá el formato HH:MM, entre 00:00 y 23:59.",
        CumplimientoError.HoraPosteriorALaActual => "La hora no puede ser posterior a la actual.",
        CumplimientoError.InstanciaYaCumplida => "La instancia ya fue cumplida.",
        CumplimientoError.InstanciaExpirada => "La instancia expiró.",
        CumplimientoError.RegistroYaEliminado => "El registro ya fue eliminado.",
        CumplimientoError.RegistroDeOtroDia => "Solo se pueden eliminar registros del día.",
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, "Error de cumplimiento desconocido."),
    };

    /// <summary>Errores de la hora ingresada: se muestran dentro del formulario, que sigue abierto.</summary>
    public static bool EsDeHora(CumplimientoError error) =>
        error is CumplimientoError.HoraObligatoria
            or CumplimientoError.HoraInvalida
            or CumplimientoError.HoraPosteriorALaActual;
}
