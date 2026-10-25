namespace PetCare.Shared.Tiempo;

/// <summary>
/// Zona horaria del hogar (NFR-03): desfase fijo de −3 h respecto de UTC, sin horario de verano.
/// Es la única conversión UTC → hora local de la app.
/// </summary>
public static class ZonaHogar
{
    public static readonly TimeSpan Desfase = TimeSpan.FromHours(-3);

    /// <summary>Hora local del hogar (<see cref="DateTimeKind.Unspecified"/>) para un instante.</summary>
    public static DateTime AhoraLocal(DateTimeOffset instanteUtc) => instanteUtc.ToOffset(Desfase).DateTime;

    /// <summary>Día local del hogar al que pertenece un instante.</summary>
    public static DateOnly Hoy(DateTimeOffset instanteUtc) => DateOnly.FromDateTime(AhoraLocal(instanteUtc));
}
