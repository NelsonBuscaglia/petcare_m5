using PetCare.Features.Instancias.Domain;

namespace PetCare.Tests.Features.Instancias;

public class ValidacionCumplimientoTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateTime A1030 = new(2026, 10, 25, 10, 30, 0);

    [Fact]
    public void Validar_HoraValida_Ok()
    {
        var resultado = ValidacionCumplimiento.Validar("10:30", Hoy, tieneRegistro: false, A1030);

        Assert.True(resultado.EsOk);
        Assert.Equal(new TimeOnly(10, 30), resultado.Valor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Validar_HoraObligatoria(string? hora)
    {
        var resultado = ValidacionCumplimiento.Validar(hora, Hoy, tieneRegistro: false, A1030);

        Assert.False(resultado.EsOk);
        Assert.Equal([CumplimientoError.HoraObligatoria], resultado.Errores);
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("9:30")]
    [InlineData("10:30\n")]
    public void Validar_HoraInvalida(string hora)
    {
        var resultado = ValidacionCumplimiento.Validar(hora, Hoy, tieneRegistro: false, A1030);

        Assert.False(resultado.EsOk);
        Assert.Equal([CumplimientoError.HoraInvalida], resultado.Errores);
    }

    [Fact]
    public void Validar_HoraPosteriorALaActual()
    {
        var a103025 = new DateTime(2026, 10, 25, 10, 30, 25);

        var posterior = ValidacionCumplimiento.Validar("10:31", Hoy, tieneRegistro: false, a103025);
        var mismoMinuto = ValidacionCumplimiento.Validar("10:30", Hoy, tieneRegistro: false, a103025);

        Assert.False(posterior.EsOk);
        Assert.Equal([CumplimientoError.HoraPosteriorALaActual], posterior.Errores);
        Assert.True(mismoMinuto.EsOk);
        Assert.Equal(new TimeOnly(10, 30), mismoMinuto.Valor);
    }

    [Fact]
    public void Validar_InstanciaExpirada()
    {
        // Expirar es lo primero que se mira: tapa el registro y la hora inválida.
        var resultado = ValidacionCumplimiento.Validar("25:00", Hoy.AddDays(-1), tieneRegistro: true, A1030);

        Assert.False(resultado.EsOk);
        Assert.Equal([CumplimientoError.InstanciaExpirada], resultado.Errores);
    }

    [Fact]
    public void Validar_InstanciaYaCumplida()
    {
        // Con registro, la hora no llega a validarse.
        var resultado = ValidacionCumplimiento.Validar(null, Hoy, tieneRegistro: true, A1030);

        Assert.False(resultado.EsOk);
        Assert.Equal([CumplimientoError.InstanciaYaCumplida], resultado.Errores);
    }
}
