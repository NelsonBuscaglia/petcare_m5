using PetCare.Features.EstadoDelDia.Ui;

namespace PetCare.Tests.Features.EstadoDelDia;

public class OpcionesRefrescoTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    public void OpcionesRefresco_FueraDeRango_Error(int segundos)
    {
        var errores = ValidacionOpcionesRefresco.Validar(new OpcionesRefresco { RefrescoSegundos = segundos });

        Assert.Equal(["EstadoDelDia:RefrescoSegundos debe estar entre 1 y 15."], errores);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(15)]
    public void OpcionesRefresco_Diez_Ok(int segundos)
    {
        Assert.Equal(10, new OpcionesRefresco().RefrescoSegundos);
        Assert.Equal("EstadoDelDia", OpcionesRefresco.Seccion);

        Assert.Empty(ValidacionOpcionesRefresco.Validar(new OpcionesRefresco { RefrescoSegundos = segundos }));
    }
}
