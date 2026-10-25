using PetCare.Shared.Resultados;

namespace PetCare.Tests.Shared;

public class ResultadoTests
{
    [Fact]
    public void Falla_ListaVacia_Lanza()
    {
        Assert.Throws<ArgumentException>(() => Resultado<int, string>.Falla([]));
    }

    [Fact]
    public void Valor_EnFalla_Lanza()
    {
        var resultado = Resultado<int, string>.Falla(["error"]);

        Assert.False(resultado.EsOk);
        Assert.Equal(["error"], resultado.Errores);
        Assert.Throws<InvalidOperationException>(() => resultado.Valor);
    }

    [Fact]
    public void Ok_ExponeValor()
    {
        var resultado = Resultado<int, string>.Ok(42);

        Assert.True(resultado.EsOk);
        Assert.Equal(42, resultado.Valor);
        Assert.Empty(resultado.Errores);
    }
}
