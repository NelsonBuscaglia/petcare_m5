using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

    [Fact]
    public async Task Eliminar_RegistroDeHoy_BorraYReactiva()
    {
        var instancia = InsertarPaseoDeHoy();
        var registro = baseDeTest.InsertarRegistro(instancia.Id, Hoy, new TimeOnly(9, 5));
        IrALocal(Hoy, 10, 0);

        var resultado = await Eliminar(registro.Id);

        Assert.True(resultado.EsOk);
        Assert.Equal(instancia.Id, resultado.Valor);
        Assert.Empty(await Registros());
        var reactivada = (await LeerInstancia(instancia.Id)).ReactivadaEnUtc;
        Assert.Equal(new DateTimeOffset(2026, 10, 25, 13, 0, 0, TimeSpan.Zero), reactivada);
        Assert.Equal(TimeSpan.Zero, reactivada?.Offset);
    }

    [Fact]
    public async Task Eliminar_RegistroDeAyer_RegistroDeOtroDia()
    {
        var ayer = Hoy.AddDays(-1);
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Paseo, Seis, Repeticion.TodosLosDias, ayer);
        var instanciaDeAyer = baseDeTest.InsertarInstancia(deber.Id, ayer, Seis);
        var registro = baseDeTest.InsertarRegistro(instanciaDeAyer.Id, ayer, Seis);
        IrALocal(Hoy, 10, 0);

        var resultado = await Eliminar(registro.Id);

        Assert.False(resultado.EsOk);
        Assert.Equal([CumplimientoError.RegistroDeOtroDia], resultado.Errores);
        Assert.Equal([registro.Id], (await Registros()).Select(r => r.Id));
        Assert.Null((await LeerInstancia(instanciaDeAyer.Id)).ReactivadaEnUtc);
    }

    [Fact]
    public async Task Eliminar_YaEliminado_RegistroYaEliminado()
    {
        var instancia = InsertarPaseoDeHoy();
        var registro = baseDeTest.InsertarRegistro(instancia.Id, Hoy, new TimeOnly(9, 5));
        IrALocal(Hoy, 10, 0);
        var primera = await Eliminar(registro.Id);
        Assert.True(primera.EsOk);

        var segunda = await Eliminar(registro.Id);
        var nuncaExistio = await Eliminar(999);

        Assert.False(segunda.EsOk);
        Assert.Equal([CumplimientoError.RegistroYaEliminado], segunda.Errores);
        Assert.False(nuncaExistio.EsOk);
        Assert.Equal([CumplimientoError.RegistroYaEliminado], nuncaExistio.Errores);
        Assert.Empty(await Registros());
    }

    [Fact]
    public async Task Eliminar_Concurrente_SoloUnoElimina()
    {
        var instancia = InsertarPaseoDeHoy();
        var registro = baseDeTest.InsertarRegistro(instancia.Id, Hoy, new TimeOnly(9, 5));
        IrALocal(Hoy, 10, 0);
        // El otro cuidador elimina el registro entre la lectura y el borrado de este.
        var interceptor = new BorraAntesDelDelete(registro.Id);
        await using var contexto = new PetCareDbContext(
            new DbContextOptionsBuilder<PetCareDbContext>()
                .UseSqlite(baseDeTest.Conexion)
                .AddInterceptors(interceptor)
                .Options);
        var servicio = new RegistrosService(contexto, reloj);

        var resultado = await servicio.EliminarAsync(registro.Id, CancellationToken.None);

        Assert.True(interceptor.Borro);
        // El servicio llegó al borrado (la lectura vio el registro) y no borró nada.
        Assert.Equal(0, interceptor.FilasDelDeleteDelServicio);
        Assert.False(resultado.EsOk);
        Assert.Equal([CumplimientoError.RegistroYaEliminado], resultado.Errores);
        Assert.Empty(await Registros());
        // Quien no borró nada no reactiva: la reactivación es del que eliminó (aquí, el otro cuidador).
        Assert.Null((await LeerInstancia(instancia.Id)).ReactivadaEnUtc);
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

    private async Task<PetCare.Shared.Resultados.Resultado<int, CumplimientoError>> Eliminar(int registroId)
    {
        await using var contexto = baseDeTest.CrearContexto();
        return await new RegistrosService(contexto, reloj).EliminarAsync(registroId, CancellationToken.None);
    }

    private async Task<Instancia> LeerInstancia(int instanciaId)
    {
        await using var contexto = baseDeTest.CrearContexto();
        return await contexto.Instancias.AsNoTracking().SingleAsync(i => i.Id == instanciaId);
    }

    private async Task<List<RegistroCumplimiento>> Registros()
    {
        await using var contexto = baseDeTest.CrearContexto();
        return await contexto.RegistrosCumplimiento.ToListAsync();
    }

    /// <summary>
    /// Simula otra eliminación del mismo registro justo antes del borrado: al cerrarse la lectura
    /// del registro (ya leído, antes de abrir la transacción y del <c>ExecuteDelete</c>), lo borra
    /// por la misma conexión y fuera de toda transacción, así que queda confirmado como lo dejaría
    /// el otro cuidador (SQLite en memoria no admite una segunda conexión sobre la misma base).
    /// Registra cuántas filas borró el <c>DELETE</c> del servicio.
    /// </summary>
    private sealed class BorraAntesDelDelete(int registroId) : DbCommandInterceptor
    {
        private int borrados;

        public bool Borro => Volatile.Read(ref borrados) > 0;

        public int? FilasDelDeleteDelServicio { get; private set; }

        public override async ValueTask<InterceptionResult> DataReaderClosingAsync(
            DbCommand command, DataReaderClosingEventData eventData, InterceptionResult result)
        {
            if (command.CommandText.Contains("FROM \"RegistrosCumplimiento\"", StringComparison.Ordinal)
                && Interlocked.Increment(ref borrados) == 1)
            {
                Assert.Null(command.Transaction);
                await using var otro = command.Connection!.CreateCommand();
                otro.CommandText = "DELETE FROM RegistrosCumplimiento WHERE Id = $id";
                var parametro = otro.CreateParameter();
                parametro.ParameterName = "$id";
                parametro.Value = registroId;
                otro.Parameters.Add(parametro);
                Assert.Equal(1, await otro.ExecuteNonQueryAsync());
            }

            return result;
        }

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                FilasDelDeleteDelServicio = result;
            }

            return ValueTask.FromResult(result);
        }
    }
}
