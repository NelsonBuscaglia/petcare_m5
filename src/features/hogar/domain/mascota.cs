namespace PetCare.Features.Hogar.Domain;

public class Mascota
{
    public int Id { get; set; }

    public int HogarId { get; set; }

    public required string Nombre { get; set; }
}
