using PetCare.Shared.Tiempo;

namespace PetCare.Tests.Shared;

public class ZonaHogarTests
{
    [Fact]
    public void Hoy_0305Utc_EsElNuevoDia()
    {
        var instante = new DateTimeOffset(2026, 10, 26, 3, 5, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2026, 10, 26), ZonaHogar.Hoy(instante));
    }

    [Fact]
    public void Hoy_0259UtcDelDiaSiguiente_EsElDiaAnterior()
    {
        var instante = new DateTimeOffset(2026, 10, 26, 2, 59, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2026, 10, 25), ZonaHogar.Hoy(instante));
    }

    [Fact]
    public void AhoraLocal_0815Utc_Es0515()
    {
        var instante = new DateTimeOffset(2026, 10, 25, 8, 15, 0, TimeSpan.Zero);

        Assert.Equal(new DateTime(2026, 10, 25, 5, 15, 0), ZonaHogar.AhoraLocal(instante));
    }
}
