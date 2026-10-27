using PetCare.Features.Emails.Domain;
using PetCare.Shared.Tiempo;

namespace PetCare.Tests.Features.Emails;

public class CondicionEmailTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Ayer = Hoy.AddDays(-1);
    private static readonly TimeOnly Nueve = new(9, 0);
    private static readonly TimeOnly Ocho = new(8, 0);

    /// <summary>Instancia de hoy 09:00, sin registro, sin email, generada antes de vencer, nunca reactivada.</summary>
    private static readonly CandidataEmail Comida0900 = new(
        InstanciaId: 1,
        Fecha: Hoy,
        Hora: Nueve,
        TieneRegistro: false,
        TieneEmailAceptado: false,
        VencidaAlGenerarse: false,
        ReactivadaEnUtc: null);

    [Fact]
    public void DebeIntentarse_AntesDeVencer_False()
    {
        Assert.False(CondicionEmail.DebeIntentarse(Comida0900, Local(Hoy, 9, 14)));
        Assert.False(CondicionEmail.DebeIntentarse(Comida0900, Local(Hoy, 9, 14, 59)));
        Assert.True(CondicionEmail.DebeIntentarse(Comida0900, Local(Hoy, 9, 15)));
    }

    [Fact]
    public void DebeIntentarse_Vencida0915_True()
    {
        Assert.True(CondicionEmail.DebeIntentarse(Comida0900, Local(Hoy, 9, 15)));
        Assert.True(CondicionEmail.DebeIntentarse(Comida0900, Local(Hoy, 9, 15, 30)));
    }

    [Fact]
    public void DebeIntentarse_FinDeVentana0920_False()
    {
        Assert.True(CondicionEmail.DebeIntentarse(Comida0900, Local(Hoy, 9, 19, 59)));
        Assert.False(CondicionEmail.DebeIntentarse(Comida0900, Local(Hoy, 9, 20)));
        Assert.False(CondicionEmail.DebeIntentarse(Comida0900, Local(Hoy, 23, 59)));
    }

    [Fact]
    public void DebeIntentarse_ConRegistro_False()
    {
        var ahora = Local(Hoy, 9, 16);
        Assert.True(CondicionEmail.DebeIntentarse(Comida0900, ahora));
        Assert.False(CondicionEmail.DebeIntentarse(Comida0900 with { TieneRegistro = true }, ahora));
    }

    [Fact]
    public void DebeIntentarse_ConEmailAceptado_False()
    {
        var ahora = Local(Hoy, 9, 16);
        Assert.True(CondicionEmail.DebeIntentarse(Comida0900, ahora));
        Assert.False(CondicionEmail.DebeIntentarse(Comida0900 with { TieneEmailAceptado = true }, ahora));
    }

    [Fact]
    public void DebeIntentarse_VencidaAlGenerarse_False()
    {
        var ahora = Local(Hoy, 9, 16);
        Assert.True(CondicionEmail.DebeIntentarse(Comida0900, ahora));
        Assert.False(CondicionEmail.DebeIntentarse(Comida0900 with { VencidaAlGenerarse = true }, ahora));
    }

    [Fact]
    public void DebeIntentarse_InstanciaDeAyer_False()
    {
        // La de ayer 23:50 vence hoy 00:05: a las 00:06 estaría en ventana, pero ya expiró.
        var deAyer = Comida0900 with { Fecha = Ayer, Hora = new TimeOnly(23, 50) };
        Assert.True(CondicionEmail.DebeIntentarse(Comida0900, Local(Hoy, 9, 16)));
        Assert.False(CondicionEmail.DebeIntentarse(deAyer, Local(Hoy, 0, 6)));
    }

    [Fact]
    public void DebeIntentarse_Reactivada1000_VentanaDesde1000()
    {
        var reactivada = Comida0900 with { Hora = Ocho, ReactivadaEnUtc = Local(Hoy, 10, 0) };

        Assert.True(CondicionEmail.DebeIntentarse(reactivada, Local(Hoy, 10, 0)));
        Assert.True(CondicionEmail.DebeIntentarse(reactivada, Local(Hoy, 10, 4)));
        Assert.False(CondicionEmail.DebeIntentarse(reactivada, Local(Hoy, 10, 5)));
    }

    [Fact]
    public void DebeIntentarse_ReactivadaAntesDeVencer_VentanaDesdeVencimiento()
    {
        // Registrada 08:05 y eliminada 08:10: la condición se cumple al vencer (08:15), no al reactivarse.
        var reactivada = Comida0900 with { Hora = Ocho, ReactivadaEnUtc = Local(Hoy, 8, 10) };

        Assert.False(CondicionEmail.DebeIntentarse(reactivada, Local(Hoy, 8, 10)));
        Assert.False(CondicionEmail.DebeIntentarse(reactivada, Local(Hoy, 8, 14)));
        Assert.True(CondicionEmail.DebeIntentarse(reactivada, Local(Hoy, 8, 15)));
        Assert.True(CondicionEmail.DebeIntentarse(reactivada, Local(Hoy, 8, 19)));
        Assert.False(CondicionEmail.DebeIntentarse(reactivada, Local(Hoy, 8, 20)));
    }

    private static DateTimeOffset Local(DateOnly fecha, int hora, int minuto, int segundo = 0) =>
        ZonaHogar.AUtc(fecha.ToDateTime(new TimeOnly(hora, minuto, segundo)));
}
