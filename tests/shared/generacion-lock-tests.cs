using PetCare.Shared.Hosting;

namespace PetCare.Tests.Shared;

public class GeneracionLockTests
{
    private static readonly TimeSpan Espera = TimeSpan.FromMilliseconds(100);

    [Fact]
    public async Task TomarAsync_SegundoEsperaHastaLiberar()
    {
        using var generacionLock = new GeneracionLock();
        var primero = await generacionLock.TomarAsync(CancellationToken.None);

        var segundo = generacionLock.TomarAsync(CancellationToken.None);
        await Task.Delay(Espera);
        Assert.False(segundo.IsCompleted);

        primero.Dispose();
        (await segundo.WaitAsync(TimeSpan.FromSeconds(5))).Dispose();
    }

    [Fact]
    public async Task Liberar_DosVeces_NoLiberaDeMas()
    {
        using var generacionLock = new GeneracionLock();
        var primero = await generacionLock.TomarAsync(CancellationToken.None);
        primero.Dispose();
        primero.Dispose();

        var segundo = await generacionLock.TomarAsync(CancellationToken.None);
        var tercero = generacionLock.TomarAsync(CancellationToken.None);
        await Task.Delay(Espera);

        Assert.False(tercero.IsCompleted);
        segundo.Dispose();
        (await tercero.WaitAsync(TimeSpan.FromSeconds(5))).Dispose();
    }
}
