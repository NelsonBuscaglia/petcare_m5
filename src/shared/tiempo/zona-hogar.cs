namespace PetCare.Shared.Tiempo;

/// <summary>
/// Zona horaria del hogar (NFR-03): desfase fijo de −3 h respecto de UTC, sin horario de verano.
/// Es la única conversión entre UTC y la hora local de la app, en los dos sentidos.
/// </summary>
public static class ZonaHogar
{
    public static readonly TimeSpan Desfase = TimeSpan.FromHours(-3);

    /// <summary>Hora local del hogar (<see cref="DateTimeKind.Unspecified"/>) para un instante.</summary>
    public static DateTime AhoraLocal(DateTimeOffset instanteUtc) => instanteUtc.ToOffset(Desfase).DateTime;

    /// <summary>Día local del hogar al que pertenece un instante.</summary>
    public static DateOnly Hoy(DateTimeOffset instanteUtc) => DateOnly.FromDateTime(AhoraLocal(instanteUtc));

    /// <summary>
    /// Instante UTC (offset cero) de una hora local del hogar. Solo acepta
    /// <see cref="DateTimeKind.Unspecified"/>: un <c>Utc</c> o <c>Local</c> es un error de programación.
    /// </summary>
    public static DateTimeOffset AUtc(DateTime local)
    {
        if (local.Kind != DateTimeKind.Unspecified)
        {
            throw new ArgumentException(
                $"Se esperaba una hora local del hogar (Kind Unspecified) y llegó Kind {local.Kind}.",
                nameof(local));
        }

        return new DateTimeOffset(local, Desfase).ToUniversalTime();
    }
}
