using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PetCare.Tests.Infra;

/// <summary>
/// Simula otra operación que crea la misma instancia entre la lectura y el guardado: ejecuta
/// <paramref name="insertar"/> (por otro contexto) justo antes del primer guardado, y solo una vez.
/// </summary>
public sealed class InsertaAntesDeGuardar(Action insertar) : SaveChangesInterceptor
{
    private int guardados;

    public bool Inserto => Volatile.Read(ref guardados) > 0;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref guardados) == 1)
        {
            insertar();
        }

        return ValueTask.FromResult(result);
    }
}
