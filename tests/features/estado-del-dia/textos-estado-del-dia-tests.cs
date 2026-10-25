using PetCare.Features.EstadoDelDia.Domain;

namespace PetCare.Tests.Features.EstadoDelDia;

public class TextosEstadoDelDiaTests
{
    [Fact]
    public void Paseos_Pluraliza()
    {
        Assert.Equal("0 paseos", TextosEstadoDelDia.Paseos(0));
        Assert.Equal("1 paseo", TextosEstadoDelDia.Paseos(1));
        Assert.Equal("2 paseos", TextosEstadoDelDia.Paseos(2));
    }

    [Fact]
    public void FechaHora_Formato()
    {
        Assert.Equal("25/10/2026 11:00", TextosEstadoDelDia.FechaHora(new UltimoRegistro(new DateOnly(2026, 10, 25), new TimeOnly(11, 0))));
        Assert.Equal("01/02/2026 07:05", TextosEstadoDelDia.FechaHora(new UltimoRegistro(new DateOnly(2026, 2, 1), new TimeOnly(7, 5))));
    }
}
