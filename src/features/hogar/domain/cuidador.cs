namespace PetCare.Features.Hogar.Domain;

public class Cuidador
{
    public int Id { get; set; }

    public int HogarId { get; set; }

    public required string Email { get; set; }
}
