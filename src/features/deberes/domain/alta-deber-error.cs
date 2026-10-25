namespace PetCare.Features.Deberes.Domain;

public enum AltaDeberError
{
    MascotaNoEncontrada,
    TipoInvalido,
    HoraObligatoria,
    HoraInvalida,
    RepeticionObligatoria,
}

public static class AltaDeberErrores
{
    /// <summary>Mensaje para el cuidador. <c>MascotaNoEncontrada</c> se responde con 404, sin mostrarlo.</summary>
    public static string Mensaje(AltaDeberError error) => error switch
    {
        AltaDeberError.MascotaNoEncontrada => "La mascota no existe.",
        AltaDeberError.TipoInvalido => "El tipo debe ser comida o paseo.",
        AltaDeberError.HoraObligatoria => "La hora es obligatoria.",
        AltaDeberError.HoraInvalida => "La hora no es válida: usá el formato HH:MM, entre 00:00 y 23:59.",
        AltaDeberError.RepeticionObligatoria => "Elegí \"solo para hoy\" o \"todos los días\".",
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, "Error de alta desconocido."),
    };
}
