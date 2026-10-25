namespace PetCare.Features.Instancias.Domain;

public class RegistroCumplimiento
{
    public int Id { get; set; }

    public int InstanciaId { get; set; }

    /// <summary>Día local del hogar (UTC−3) del cumplimiento.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Hora local real del cumplimiento (UTC−3).</summary>
    public TimeOnly HoraReal { get; set; }
}
