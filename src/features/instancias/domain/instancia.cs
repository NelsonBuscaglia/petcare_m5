namespace PetCare.Features.Instancias.Domain;

public class Instancia
{
    public int Id { get; set; }

    public int DeberId { get; set; }

    /// <summary>Día local del hogar (UTC−3) al que pertenece.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Copia de <c>Deber.Hora</c> al momento de generarse.</summary>
    public TimeOnly Hora { get; set; }

    /// <summary>
    /// Ya estaba vencida cuando se creó (alta con hora pasada o generación tardía, FR-14): no
    /// dispara el email de vencimiento.
    /// </summary>
    public bool VencidaAlGenerarse { get; set; }

    /// <summary>Último momento en que se eliminó su registro y volvió a ser vigente (UTC, offset cero).</summary>
    public DateTimeOffset? ReactivadaEnUtc { get; set; }
}
