namespace PetCare.Shared.Hosting;

/// <summary>
/// Exclusión mutua entre el alta de deberes y la generación diaria de instancias, para que no
/// creen a la vez la misma instancia (índice único DeberId + Fecha). Se registra como singleton.
/// </summary>
public sealed class GeneracionLock : IDisposable
{
    private readonly SemaphoreSlim semaforo = new(1, 1);

    /// <summary>Espera el lock; se libera al hacer <c>Dispose</c> del valor devuelto.</summary>
    public async Task<IDisposable> TomarAsync(CancellationToken cancellationToken)
    {
        await semaforo.WaitAsync(cancellationToken);
        return new Liberacion(semaforo);
    }

    public void Dispose() => semaforo.Dispose();

    private sealed class Liberacion(SemaphoreSlim semaforo) : IDisposable
    {
        private int liberado;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref liberado, 1) == 0)
            {
                semaforo.Release();
            }
        }
    }
}
