using PetCare.Features.Instancias.Domain;
using PetCare.Shared.Tiempo;

namespace PetCare.Features.Emails.Domain;

/// <summary>Lo que hace falta saber de una instancia para decidir si se intenta su email de vencimiento.</summary>
public sealed record CandidataEmail(
    int InstanciaId,
    DateOnly Fecha,
    TimeOnly Hora,
    bool TieneRegistro,
    bool TieneEmailAceptado,
    bool VencidaAlGenerarse,
    DateTimeOffset? ReactivadaEnUtc);

public static class CondicionEmail
{
    /// <summary>Ventana de reintentos desde que se cumple la condición que dispara el email (FR-16).</summary>
    public static readonly TimeSpan Ventana = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Si en <paramref name="ahoraUtc"/> corresponde intentar el email de vencimiento (FR-13, FR-14,
    /// FR-16, FR-17). La condición se cumple al vencer o, si la instancia se reactivó después, al
    /// reactivarse; desde ahí hay <see cref="Ventana"/> para lograr que el servicio lo acepte.
    /// </summary>
    public static bool DebeIntentarse(CandidataEmail c, DateTimeOffset ahoraUtc)
    {
        if (c.TieneRegistro || c.TieneEmailAceptado || c.VencidaAlGenerarse || c.Fecha != ZonaHogar.Hoy(ahoraUtc))
        {
            return false;
        }

        var vencimientoUtc = ZonaHogar.AUtc(EstadoInstancia.MomentoDeVencimiento(c.Fecha, c.Hora));
        if (ahoraUtc < vencimientoUtc)
        {
            return false;
        }

        var desde = c.ReactivadaEnUtc is { } reactivada && reactivada > vencimientoUtc ? reactivada : vencimientoUtc;
        return ahoraUtc < desde + Ventana;
    }
}
