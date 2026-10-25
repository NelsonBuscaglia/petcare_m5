using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Instancias.Data;
using PetCare.Features.Instancias.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Hosting;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.Instancias;

public sealed class InstanciasServiceTests : IDisposable
{
    private const int Firulais = 1;

    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Manana = Hoy.AddDays(1);
    private static readonly TimeOnly Ocho = new(8, 0);

    private readonly BaseDeTest baseDeTest = BaseDeTest.Crear();
    private readonly FakeTimeProvider reloj = new(PetCareAppFactory.InstanteInicial);
    private readonly GeneracionLock generacionLock = new();

    public void Dispose()
    {
        generacionLock.Dispose();
        baseDeTest.Dispose();
    }

    [Fact]
    public async Task GenerarDelDia_0005_CreaInstanciaDelNuevoDia()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Ocho, Repeticion.TodosLosDias, Hoy);
        baseDeTest.InsertarInstancia(deber.Id, Hoy, Ocho);
        reloj.SetUtcNow(new DateTimeOffset(2026, 10, 26, 3, 5, 0, TimeSpan.Zero));

        var resultado = await Generar();

        Assert.True(resultado.EsOk);
        Assert.Equal(1, resultado.Valor);
        Assert.Contains((deber.Id, Manana, Ocho), await Instancias());
    }

    [Fact]
    public async Task GenerarDelDia_2359_NoCreaInstanciaDeManana()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Ocho, Repeticion.TodosLosDias, Hoy);
        reloj.SetUtcNow(new DateTimeOffset(2026, 10, 26, 2, 59, 0, TimeSpan.Zero));

        var resultado = await Generar();

        Assert.True(resultado.EsOk);
        Assert.Equal([(deber.Id, Hoy, Ocho)], await Instancias());
    }

    [Fact]
    public async Task GenerarDelDia_DosVeces_UnaSolaInstancia()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Ocho, Repeticion.TodosLosDias, Hoy);

        var primera = await Generar();
        var segunda = await Generar();

        Assert.Equal(1, primera.Valor);
        Assert.Equal(0, segunda.Valor);
        Assert.Equal([(deber.Id, Hoy, Ocho)], await Instancias());
    }

    [Fact]
    public async Task GenerarDelDia_ChoqueDeIndiceUnico_DevuelveInstanciaYaExistia()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Ocho, Repeticion.TodosLosDias, Hoy);
        var interceptor = new InsertaAntesDeGuardar(() => baseDeTest.InsertarInstancia(deber.Id, Hoy, Ocho));
        await using var contexto = new PetCareDbContext(
            new DbContextOptionsBuilder<PetCareDbContext>()
                .UseSqlite(baseDeTest.Conexion)
                .AddInterceptors(interceptor)
                .Options);
        var servicio = new InstanciasService(contexto, reloj, generacionLock);

        var resultado = await servicio.GenerarDelDiaAsync(CancellationToken.None);

        Assert.True(interceptor.Inserto);
        Assert.False(resultado.EsOk);
        Assert.Equal([GeneracionError.InstanciaYaExistia], resultado.Errores);
        Assert.Empty(contexto.ChangeTracker.Entries());
        Assert.Equal([(deber.Id, Hoy, Ocho)], await Instancias());
    }

    private async Task<PetCare.Shared.Resultados.Resultado<int, GeneracionError>> Generar()
    {
        await using var contexto = baseDeTest.CrearContexto();
        return await new InstanciasService(contexto, reloj, generacionLock).GenerarDelDiaAsync(CancellationToken.None);
    }

    private async Task<List<(int DeberId, DateOnly Fecha, TimeOnly Hora)>> Instancias()
    {
        await using var contexto = baseDeTest.CrearContexto();
        var filas = await contexto.Instancias.OrderBy(i => i.Fecha).ToListAsync();
        return filas.Select(i => (i.DeberId, i.Fecha, i.Hora)).ToList();
    }
}
