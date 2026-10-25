namespace PetCare.Features.Instancias.Domain;

public enum GeneracionError
{
    /// <summary>Otra operación creó la misma instancia (DeberId + Fecha) entre la lectura y el guardado.</summary>
    InstanciaYaExistia,
}
