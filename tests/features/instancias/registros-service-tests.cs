using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Instancias.Data;
using PetCare.Features.Instancias.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Tiempo;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.Instancias;

public sealed class RegistrosServiceTests : IDisposable
{
    private const int Firulais = 1;

    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly TimeOnly Seis = new(18, 0);

    private readonly BaseDeTest baseDeTest = BaseDeTest.Crear();
    private readonly FakeTimeProvider reloj = new(PetCareAppFactory.InstanteInicial);

    public void Dispose() => baseDeTest.Dispose();

    [Fact]
    public async Task Registrar_InstanciaVigente_CreaRegistro()
    {
        var instancia = InsertarPaseoDeHoy();
        IrALocal(Hoy, 10, 30);

        var resultado = await Registrar(instancia.Id, "10:15");

        Assert.True(resultado.EsOk);
        var registro = Assert.Single(await Registros());
        Assert.Equal(resultado.Valor, registro.Id);
        Assert.Equal((instancia.Id, Hoy, new TimeOnly(10, 15)), (registro.InstanciaId, registro.Fecha, registro.HoraReal));
    }

    [Fact]
    public async Task Registrar_InstanciaInexistente_InstanciaNoEncontrada()
    {
        var resultado = await Registrar(999, "09:00");

        Assert.False(resultado.EsOk);
        Assert.Equal([CumplimientoError.InstanciaNoEncontrada], resultado.Errores);
        Assert.Empty(await Registros());
    }

    [Fact]
    public async Task Registrar_InstanciaDeAyer_InstanciaExpirada()
    {
        var ayer = Hoy.AddDays(-1);
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Paseo, Seis, Repeticion.TodosLosDias, ayer);
        var instanciaDeAyer = baseDeTest.InsertarInstancia(deber.Id, ayer, Seis);

        var resultado = await Registrar(instanciaDeAyer.Id, "08:00");

        Assert.False(resultado.EsOk);
        Assert.Equal([CumplimientoError.InstanciaExpirada], resultado.Errores);
        Assert.Empty(await Registros());
    }

    [Fact]
    public async Task Registrar_Concurrente_SegundoRecibeYaCumplida()
    {
        var instancia = InsertarPaseoDeHoy();
        IrALocal(Hoy, 10, 30);
        // El otro cuidador registra entre la lectura (sin registro) y el guardado de este.
        var interceptor = new InsertaAntesDeGuardar(() => baseDeTest.InsertarRegistro(instancia.Id, Hoy, new TimeOnly(10, 0)));
        await using var contexto = new PetCareDbContext(
            new DbContextOptionsBuilder<PetCareDbContext>()
                .UseSqlite(baseDeTest.Conexion)
                .AddInterceptors(interceptor)
                .Options);
        var servicio = new RegistrosService(contexto, reloj);

        var resultado = await servicio.RegistrarAsync(instancia.Id, "10:20", CancellationToken.None);

        Assert.True(interceptor.Inserto);
        Assert.False(resultado.EsOk);
        Assert.Equal([CumplimientoError.InstanciaYaCumplida], resultado.Errores);
        Assert.Empty(contexto.ChangeTracker.Entries());
        var registro = Assert.Single(await Registros());
        Assert.Equal(new TimeOnly(10, 0), registro.HoraReal);
    }

    [Fact]
    public async Task PrepararFormulario_PropuestaEsHoraActual()
    {
        var instancia = InsertarPaseoDeHoy();
        reloj.SetUtcNow(ZonaHogar.AUtc(Hoy.ToDateTime(new TimeOnly(10, 30, 40))));

        await using var contexto = baseDeTest.CrearContexto();
        var resultado = await new RegistrosService(contexto, reloj).PrepararFormularioAsync(instancia.Id, CancellationToken.None);

        Assert.True(resultado.EsOk);
        // Truncada al minuto: los 40 segundos no se proponen.
        Assert.Equal(new TimeOnly(10, 30), resultado.Valor);
    }

    private Instancia InsertarPaseoDeHoy()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Paseo, Seis, Repeticion.SoloHoy, Hoy);
        return baseDeTest.InsertarInstancia(deber.Id, Hoy, Seis);
    }

    private void IrALocal(DateOnly dia, int hora, int minuto) =>
        reloj.SetUtcNow(ZonaHogar.AUtc(dia.ToDateTime(new TimeOnly(hora, minuto))));

    private async Task<PetCare.Shared.Resultados.Resultado<int, CumplimientoError>> Registrar(int instanciaId, string? horaReal)
    {
        await using var contexto = baseDeTest.CrearContexto();
        return await new RegistrosService(contexto, reloj).RegistrarAsync(instanciaId, horaReal, CancellationToken.None);
    }

    private async Task<List<RegistroCumplimiento>> Registros()
    {
        await using var contexto = baseDeTest.CrearContexto();
        return await contexto.RegistrosCumplimiento.ToListAsync();
    }
}
