using PetCare.Features.Deberes.Domain;

namespace PetCare.Tests.Features.Deberes;

public class ValidacionAltaDeberTests
{
    [Fact]
    public void Validar_PaseoHoraTodosLosDias_Ok()
    {
        var resultado = ValidacionAltaDeber.Validar("paseo", "18:00", "todos-los-dias");

        Assert.True(resultado.EsOk);
        Assert.Equal(new AltaDeberValida(TipoDeber.Paseo, new TimeOnly(18, 0), Repeticion.TodosLosDias), resultado.Valor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Validar_SinHora_HoraObligatoria(string? hora)
    {
        var resultado = ValidacionAltaDeber.Validar("comida", hora, "solo-hoy");

        Assert.False(resultado.EsOk);
        Assert.Equal([AltaDeberError.HoraObligatoria], resultado.Errores);
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("12:60")]
    [InlineData("9:00")]
    [InlineData("123456")]
    [InlineData("18:00\n")]
    public void Validar_Hora2500_HoraInvalida(string hora)
    {
        var resultado = ValidacionAltaDeber.Validar("comida", hora, "solo-hoy");

        Assert.False(resultado.EsOk);
        Assert.Equal([AltaDeberError.HoraInvalida], resultado.Errores);
    }

    [Theory]
    [InlineData("medicación")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("paseopaseopaseopaseop")]
    public void Validar_TipoMedicacionOVacio_TipoInvalido(string? tipo)
    {
        var resultado = ValidacionAltaDeber.Validar(tipo, "08:00", "solo-hoy");

        Assert.False(resultado.EsOk);
        Assert.Equal([AltaDeberError.TipoInvalido], resultado.Errores);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("semanal")]
    public void Validar_SinRepeticion_RepeticionObligatoria(string? repeticion)
    {
        var resultado = ValidacionAltaDeber.Validar("comida", "08:00", repeticion);

        Assert.False(resultado.EsOk);
        Assert.Equal([AltaDeberError.RepeticionObligatoria], resultado.Errores);
    }

    [Fact]
    public void Validar_VariosErrores_DevuelveTodos()
    {
        var resultado = ValidacionAltaDeber.Validar("medicación", "25:00", "todos-los-dias");

        Assert.False(resultado.EsOk);
        Assert.Equal([AltaDeberError.TipoInvalido, AltaDeberError.HoraInvalida], resultado.Errores);
    }
}
