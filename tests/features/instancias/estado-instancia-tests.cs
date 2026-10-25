using PetCare.Features.Instancias.Domain;

namespace PetCare.Tests.Features.Instancias;

public class EstadoInstanciaTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Ayer = Hoy.AddDays(-1);
    private static readonly TimeOnly Nueve = new(9, 0);

    private static DateTime Local(DateOnly fecha, int hora, int minuto) => fecha.ToDateTime(new TimeOnly(hora, minuto));

    [Fact]
    public void EsVencida_0910_False() =>
        Assert.False(EstadoInstancia.EsVencida(Hoy, Nueve, Local(Hoy, 9, 10)));

    [Fact]
    public void EsVencida_0915_True()
    {
        Assert.True(EstadoInstancia.EsVencida(Hoy, Nueve, Local(Hoy, 9, 15)));

        // Instancia de ayer: la hora de ahora es anterior a su horario, pero el día ya pasó.
        Assert.True(EstadoInstancia.EsVencida(Ayer, Nueve, Local(Hoy, 8, 0)));
    }

    [Fact]
    public void EsVencida_0515Para0800_False() =>
        Assert.False(EstadoInstancia.EsVencida(Hoy, new TimeOnly(8, 0), Local(Hoy, 5, 15)));

    [Fact]
    public void EsVencida_2350Alas0005DelDiaSiguiente_True() =>
        Assert.True(EstadoInstancia.EsVencida(Hoy, new TimeOnly(23, 50), Local(Hoy.AddDays(1), 0, 5)));

    [Fact]
    public void EsVencida_2350Alas0004DelDiaSiguiente_False()
    {
        Assert.False(EstadoInstancia.EsVencida(Hoy, new TimeOnly(23, 50), Local(Hoy.AddDays(1), 0, 4)));

        // 00:05 del mismo día de la instancia de las 23:50: faltan casi 24 h para su horario.
        Assert.False(EstadoInstancia.EsVencida(Hoy, new TimeOnly(23, 50), Local(Hoy, 0, 5)));
    }

    [Fact]
    public void EsVigente_InstanciaDeAyer_False()
    {
        Assert.False(EstadoInstancia.EsVigente(Ayer, tieneRegistro: false, Hoy));
        Assert.True(EstadoInstancia.EsVigente(Hoy, tieneRegistro: false, Hoy));
    }

    [Fact]
    public void EsVigente_ConRegistro_False() =>
        Assert.False(EstadoInstancia.EsVigente(Hoy, tieneRegistro: true, Hoy));

    [Fact]
    public void MomentoDeVencimiento_Suma15Minutos()
    {
        Assert.Equal(new DateTime(2026, 10, 25, 9, 15, 0), EstadoInstancia.MomentoDeVencimiento(Hoy, Nueve));

        // Cruza la medianoche: 23:50 vence a las 00:05 del día siguiente.
        Assert.Equal(new DateTime(2026, 10, 26, 0, 5, 0), EstadoInstancia.MomentoDeVencimiento(Hoy, new TimeOnly(23, 50)));
    }
}
