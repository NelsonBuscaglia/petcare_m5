namespace PetCare.Features.Instancias.Domain;

/// <summary>
/// Vencimiento (FR-10) y expiración (FR-11) de una instancia: no se guardan, se calculan al leer
/// contra la hora local del hogar.
/// </summary>
public static class EstadoInstancia
{
    private static readonly TimeSpan Tolerancia = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Hora local en que vence: 15 minutos después de su horario. Se combinan fecha y hora: sumar
    /// sobre <see cref="TimeOnly"/> daría la vuelta a medianoche (23:50 + 15 min = 00:05 del mismo día).
    /// </summary>
    public static DateTime MomentoDeVencimiento(DateOnly fecha, TimeOnly hora) =>
        fecha.ToDateTime(hora).Add(Tolerancia);

    /// <summary>Vencida cuando la hora local alcanzó su <see cref="MomentoDeVencimiento"/>.</summary>
    public static bool EsVencida(DateOnly fecha, TimeOnly hora, DateTime ahoraLocal) =>
        ahoraLocal >= MomentoDeVencimiento(fecha, hora);

    /// <summary>Vigente: es de hoy y no tiene registro de cumplimiento. Las de días anteriores expiraron.</summary>
    public static bool EsVigente(DateOnly fecha, bool tieneRegistro, DateOnly hoy) =>
        !tieneRegistro && fecha == hoy;
}
