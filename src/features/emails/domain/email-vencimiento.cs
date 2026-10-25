namespace PetCare.Features.Emails.Domain;

/// <summary>Email de vencimiento aceptado por el servidor para una instancia (como mucho uno, FR-17).</summary>
public class EmailVencimiento
{
    public int Id { get; set; }

    public int InstanciaId { get; set; }

    /// <summary>Instante en que el envío fue aceptado (UTC, offset cero).</summary>
    public DateTimeOffset AceptadoEnUtc { get; set; }
}
