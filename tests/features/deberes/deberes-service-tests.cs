using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using PetCare.Features.Deberes.Data;
using PetCare.Features.Deberes.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Hosting;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.Deberes;

public sealed class DeberesServiceTests : IDisposable
{
    private const int Firulais = 1;
    private const int Michi = 2;

    /// <summary>25/10/2026 en UTC−3, el día del reloj inicial.</summary>
    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Ayer = Hoy.AddDays(-1);

    private readonly BaseDeTest baseDeTest = BaseDeTest.Crear();
    private readonly FakeTimeProvider reloj = new(PetCareAppFactory.InstanteInicial);
    private readonly GeneracionLock generacionLock = new();

    public void Dispose()
    {
        generacionLock.Dispose();
        baseDeTest.Dispose();
    }

    [Fact]
    public async Task DarDeAlta_PaseoTodosLosDias_ApareceEnActivos()
    {
        await using var contexto = baseDeTest.CrearContexto();
        var servicio = CrearServicio(contexto);

        var resultado = await servicio.DarDeAltaAsync(Firulais, "paseo", "18:00", "todos-los-dias", CancellationToken.None);
        var activos = await servicio.ListarActivosAsync(Firulais, CancellationToken.None);

        Assert.True(resultado.EsOk);
        var activo = Assert.Single(activos);
        Assert.Equal(new DeberActivoVista(resultado.Valor, TipoDeber.Paseo, new TimeOnly(18, 0), Repeticion.TodosLosDias), activo);
    }

    [Fact]
    public async Task DarDeAlta_ParaFirulais_NoApareceEnMichi()
    {
        await using var contexto = baseDeTest.CrearContexto();
        var servicio = CrearServicio(contexto);

        var resultado = await servicio.DarDeAltaAsync(Firulais, "paseo", "09:00", "solo-hoy", CancellationToken.None);
        var deFirulais = await servicio.ListarActivosAsync(Firulais, CancellationToken.None);
        var deMichi = await servicio.ListarActivosAsync(Michi, CancellationToken.None);

        Assert.True(resultado.EsOk);
        Assert.Equal([resultado.Valor], deFirulais.Select(d => d.Id));
        Assert.Empty(deMichi);
        await using var verificacion = baseDeTest.CrearContexto();
        Assert.Equal(Firulais, (await verificacion.Deberes.SingleAsync()).MascotaId);
    }

    [Fact]
    public async Task DarDeAlta_MascotaInexistente_MascotaNoEncontrada()
    {
        await using var contexto = baseDeTest.CrearContexto();
        var servicio = CrearServicio(contexto);

        var resultado = await servicio.DarDeAltaAsync(999, "paseo", "18:00", "todos-los-dias", CancellationToken.None);

        Assert.False(resultado.EsOk);
        Assert.Equal([AltaDeberError.MascotaNoEncontrada], resultado.Errores);
        await using var verificacion = baseDeTest.CrearContexto();
        Assert.Equal(0, await verificacion.Deberes.CountAsync());
    }

    [Fact]
    public async Task DarDeAlta_DatosInvalidos_NoEscribeNada()
    {
        await using var contexto = baseDeTest.CrearContexto();
        var servicio = CrearServicio(contexto);

        var resultado = await servicio.DarDeAltaAsync(Firulais, "medicación", "25:00", null, CancellationToken.None);

        Assert.False(resultado.EsOk);
        Assert.Equal(
            [AltaDeberError.TipoInvalido, AltaDeberError.HoraInvalida, AltaDeberError.RepeticionObligatoria],
            resultado.Errores);
        await using var verificacion = baseDeTest.CrearContexto();
        Assert.Equal(0, await verificacion.Deberes.CountAsync());
        Assert.Equal(0, await verificacion.Instancias.CountAsync());
    }

    [Fact]
    public async Task DarDeAlta_HoraYaPasada_GeneraInstanciaDeHoy()
    {
        // 13:30 UTC = 10:30 en UTC−3.
        reloj.SetUtcNow(new DateTimeOffset(2026, 10, 25, 13, 30, 0, TimeSpan.Zero));
        await using var contexto = baseDeTest.CrearContexto();
        var servicio = CrearServicio(contexto);

        var resultado = await servicio.DarDeAltaAsync(Firulais, "paseo", "10:00", "solo-hoy", CancellationToken.None);

        Assert.True(resultado.EsOk);
        await using var verificacion = baseDeTest.CrearContexto();
        var deber = await verificacion.Deberes.SingleAsync();
        Assert.Equal(Hoy, deber.FechaAlta);
        var instancia = await verificacion.Instancias.SingleAsync();
        Assert.Equal(resultado.Valor, instancia.DeberId);
        Assert.Equal(Hoy, instancia.Fecha);
        Assert.Equal(new TimeOnly(10, 0), instancia.Hora);
    }

    [Fact]
    public async Task DarDeAlta_HoraYaVencida_MarcaVencidaAlGenerarse()
    {
        // 13:30 UTC = 10:30 en UTC−3: el paseo de las 10:00 venció a las 10:15.
        reloj.SetUtcNow(new DateTimeOffset(2026, 10, 25, 13, 30, 0, TimeSpan.Zero));
        await using var contexto = baseDeTest.CrearContexto();

        var resultado = await CrearServicio(contexto).DarDeAltaAsync(Firulais, "paseo", "10:00", "solo-hoy", CancellationToken.None);

        Assert.True(resultado.EsOk);
        await using var verificacion = baseDeTest.CrearContexto();
        var instancia = await verificacion.Instancias.SingleAsync();
        Assert.True(instancia.VencidaAlGenerarse);
        Assert.Null(instancia.ReactivadaEnUtc);
    }

    [Fact]
    public async Task DarDeAlta_HoraFutura_NoMarcaVencidaAlGenerarse()
    {
        // Reloj inicial: 12:00 UTC = 09:00 en UTC−3.
        await using var contexto = baseDeTest.CrearContexto();

        var resultado = await CrearServicio(contexto).DarDeAltaAsync(Firulais, "paseo", "18:00", "todos-los-dias", CancellationToken.None);

        Assert.True(resultado.EsOk);
        await using var verificacion = baseDeTest.CrearContexto();
        var instancia = await verificacion.Instancias.SingleAsync();
        Assert.Equal(new TimeOnly(18, 0), instancia.Hora);
        Assert.False(instancia.VencidaAlGenerarse);
        Assert.Null(instancia.ReactivadaEnUtc);
    }

    [Fact]
    public async Task ListarActivos_SoloHoyDeAyerNoCumplido_NoAparece()
    {
        var deAyer = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, new TimeOnly(8, 0), Repeticion.SoloHoy, Ayer);
        baseDeTest.InsertarInstancia(deAyer.Id, Ayer, new TimeOnly(8, 0));
        var deHoy = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, new TimeOnly(20, 0), Repeticion.SoloHoy, Hoy);
        baseDeTest.InsertarInstancia(deHoy.Id, Hoy, new TimeOnly(20, 0));
        await using var contexto = baseDeTest.CrearContexto();

        var activos = await CrearServicio(contexto).ListarActivosAsync(Firulais, CancellationToken.None);

        Assert.Equal([deHoy.Id], activos.Select(d => d.Id));
    }

    [Fact]
    public async Task ListarActivos_SoloHoyConRegistro_NoAparece()
    {
        var cumplido = baseDeTest.InsertarDeber(Firulais, TipoDeber.Paseo, new TimeOnly(7, 0), Repeticion.SoloHoy, Hoy);
        var instancia = baseDeTest.InsertarInstancia(cumplido.Id, Hoy, new TimeOnly(7, 0));
        baseDeTest.InsertarRegistro(instancia.Id, Hoy, new TimeOnly(7, 5));
        var pendiente = baseDeTest.InsertarDeber(Firulais, TipoDeber.Paseo, new TimeOnly(19, 0), Repeticion.SoloHoy, Hoy);
        baseDeTest.InsertarInstancia(pendiente.Id, Hoy, new TimeOnly(19, 0));
        await using var contexto = baseDeTest.CrearContexto();

        var activos = await CrearServicio(contexto).ListarActivosAsync(Firulais, CancellationToken.None);

        Assert.Equal([pendiente.Id], activos.Select(d => d.Id));
    }

    [Fact]
    public async Task ListarActivos_TodosLosDiasConAyerExpirada_SigueApareciendo()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Paseo, new TimeOnly(9, 0), Repeticion.TodosLosDias, Ayer);
        baseDeTest.InsertarInstancia(deber.Id, Ayer, new TimeOnly(9, 0));
        await using var contexto = baseDeTest.CrearContexto();

        var activos = await CrearServicio(contexto).ListarActivosAsync(Firulais, CancellationToken.None);

        Assert.Equal(
            [new DeberActivoVista(deber.Id, TipoDeber.Paseo, new TimeOnly(9, 0), Repeticion.TodosLosDias)],
            activos);
    }

    private DeberesService CrearServicio(PetCareDbContext contexto) =>
        new(contexto, reloj, generacionLock);
}
