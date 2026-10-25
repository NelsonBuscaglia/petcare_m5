using PetCare.Features.Deberes.Domain;

namespace PetCare.Features.Instancias.Domain;

public sealed record DeberParaGenerar(int Id, TimeOnly Hora, Repeticion Repeticion, DateOnly FechaAlta);

public sealed record InstanciaAGenerar(int DeberId, DateOnly Fecha, TimeOnly Hora);

public static class GeneracionInstancias
{
    /// <summary>
    /// Instancias que faltan para <paramref name="dia"/> (FR-08, FR-09): una por cada deber "todos
    /// los días" ya dado de alta ese día que todavía no la tiene. Los "solo para hoy" no entran:
    /// su única instancia la crea el alta (FR-07).
    /// </summary>
    public static IReadOnlyList<InstanciaAGenerar> QueFaltan(
        DateOnly dia,
        IEnumerable<DeberParaGenerar> deberes,
        IReadOnlySet<int> deberesConInstanciaEseDia) =>
        deberes
            .Where(d => d.Repeticion == Repeticion.TodosLosDias
                && d.FechaAlta <= dia
                && !deberesConInstanciaEseDia.Contains(d.Id))
            .Select(d => new InstanciaAGenerar(d.Id, dia, d.Hora))
            .ToList();
}
