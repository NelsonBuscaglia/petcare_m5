using PetCare.Shared.Tiempo;

namespace PetCare.Tests.Shared;

public class FormatoHoraTests
{
    [Fact]
    public void FormatoHora_TryParse()
    {
        Assert.True(FormatoHora.TryParse("00:00", out var medianoche));
        Assert.Equal(new TimeOnly(0, 0), medianoche);
        Assert.True(FormatoHora.TryParse("23:59", out var ultimoMinuto));
        Assert.Equal(new TimeOnly(23, 59), ultimoMinuto);

        // `18:00\n`: `$` lo aceptaría; el patrón termina en `\z`.
        foreach (var invalida in new[] { "24:00", "9:00", "12:60", "18:00\n", "", null })
        {
            Assert.False(FormatoHora.TryParse(invalida, out var hora), $"Aceptó {invalida ?? "null"}");
            Assert.Equal(default, hora);
        }
    }
}
