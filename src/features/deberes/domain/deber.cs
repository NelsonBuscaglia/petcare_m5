namespace PetCare.Features.Deberes.Domain;

public class Deber
{
    public int Id { get; set; }

    public int MascotaId { get; set; }

    public TipoDeber Tipo { get; set; }

    /// <summary>Hora local del hogar (UTC−3).</summary>
    public TimeOnly Hora { get; set; }

    public Repeticion Repeticion { get; set; }

    /// <summary>Día local del hogar (UTC−3) en que se dio de alta.</summary>
    public DateOnly FechaAlta { get; set; }
}
