namespace PetCare.Features.Instancias.Domain;

public class Instancia
{
    public int Id { get; set; }

    public int DeberId { get; set; }

    /// <summary>Día local del hogar (UTC−3) al que pertenece.</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Copia de <c>Deber.Hora</c> al momento de generarse.</summary>
    public TimeOnly Hora { get; set; }
}
