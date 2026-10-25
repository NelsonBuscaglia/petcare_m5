using PetCare.Features.Deberes.Domain;
using PetCare.Features.Instancias.Domain;

namespace PetCare.Tests.Features.Instancias;

public class GeneracionInstanciasTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Manana = Hoy.AddDays(1);
    private static readonly HashSet<int> Ninguno = [];

    [Fact]
    public void QueFaltan_TodosLosDias_IncluyeManana()
    {
        var deber = new DeberParaGenerar(7, new TimeOnly(9, 0), Repeticion.TodosLosDias, Hoy);

        var faltan = GeneracionInstancias.QueFaltan(Manana, [deber], Ninguno);

        Assert.Equal([new InstanciaAGenerar(7, Manana, new TimeOnly(9, 0))], faltan);
    }

    [Fact]
    public void QueFaltan_SoloHoy_NoGeneraManana()
    {
        var soloHoy = new DeberParaGenerar(7, new TimeOnly(20, 0), Repeticion.SoloHoy, Hoy);
        var todosLosDias = new DeberParaGenerar(8, new TimeOnly(9, 0), Repeticion.TodosLosDias, Hoy);

        var faltan = GeneracionInstancias.QueFaltan(Manana, [soloHoy, todosLosDias], Ninguno);

        Assert.Equal([8], faltan.Select(i => i.DeberId));
    }

    [Fact]
    public void QueFaltan_FechaAltaPosterior_NoIncluye()
    {
        var deManana = new DeberParaGenerar(7, new TimeOnly(9, 0), Repeticion.TodosLosDias, Manana);
        var deHoy = new DeberParaGenerar(8, new TimeOnly(9, 0), Repeticion.TodosLosDias, Hoy);

        var faltan = GeneracionInstancias.QueFaltan(Hoy, [deManana, deHoy], Ninguno);

        Assert.Equal([8], faltan.Select(i => i.DeberId));
    }

    [Fact]
    public void QueFaltan_YaTieneInstancia_NoIncluye()
    {
        var conInstancia = new DeberParaGenerar(7, new TimeOnly(9, 0), Repeticion.TodosLosDias, Hoy);
        var sinInstancia = new DeberParaGenerar(8, new TimeOnly(10, 0), Repeticion.TodosLosDias, Hoy);

        var faltan = GeneracionInstancias.QueFaltan(Manana, [conInstancia, sinInstancia], new HashSet<int> { 7 });

        Assert.Equal([new InstanciaAGenerar(8, Manana, new TimeOnly(10, 0))], faltan);
    }
}
