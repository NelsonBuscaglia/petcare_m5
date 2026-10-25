using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Instancias.Data;
using PetCare.Features.Instancias.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Hosting;
using PetCare.Shared.Tiempo;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.Instancias;

public sealed class GeneracionDiariaWorkerTests : IDisposable
{
    private const int Firulais = 1;
    private const int EventoCompletada = 3000;
    private const int EventoYaExistia = 3001;
    private const int EventoFallida = 3002;

    private static readonly DateOnly Ayer = new(2026, 10, 24);
    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Manana = Hoy.AddDays(1);
    private static readonly TimeOnly Ocho = new(8, 0);
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(5);

    /// <summary>25/10/2026 10:00 en UTC−3.</summary>
    private static readonly DateTimeOffset DiezDeHoy = new(2026, 10, 25, 13, 0, 0, TimeSpan.Zero);

    private readonly BaseDeTest baseDeTest = BaseDeTest.Crear();
    private readonly FakeTimeProvider reloj = new(DiezDeHoy);
    private readonly LoggerDePrueba logger = new();
    private ServiceProvider? proveedor;

    public void Dispose() => baseDeTest.Dispose();

    [Fact]
    public async Task Worker_ArrancaA1000TrasCaida_GeneraInstanciaYQuedaPendiente()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Ocho, Repeticion.TodosLosDias, Ayer);
        baseDeTest.InsertarInstancia(deber.Id, Ayer, Ocho);
        await using var servicios = CrearProveedor();
        using var worker = CrearWorker(servicios);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            Assert.True(await EsperarAsync(() => Task.FromResult(logger.Pasadas() >= 1)), "No terminó la pasada inicial.");
            var deHoy = Assert.Single(await InstanciasDe(Hoy));
            Assert.Equal((deber.Id, Ocho), (deHoy.DeberId, deHoy.Hora));
            Assert.True(EstadoInstancia.EsVencida(deHoy.Fecha, deHoy.Hora, ZonaHogar.AhoraLocal(reloj.GetUtcNow())));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Worker_Reinicio_NoDuplica()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Ocho, Repeticion.TodosLosDias, Hoy);
        baseDeTest.InsertarInstancia(deber.Id, Hoy, Ocho);
        await using var servicios = CrearProveedor();
        using var worker = CrearWorker(servicios);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            Assert.True(await EsperarAsync(() => Task.FromResult(logger.Pasadas() >= 1)), "No terminó la pasada inicial.");
            Assert.Single(await InstanciasDe(Hoy));
            Assert.Empty(logger.Errores());
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Worker_CruzaMedianoche_GeneraEnMenosDe5Minutos()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Ocho, Repeticion.TodosLosDias, Hoy);
        reloj.SetUtcNow(new DateTimeOffset(2026, 10, 26, 2, 59, 0, TimeSpan.Zero));
        var limite = Manana.ToDateTime(new TimeOnly(0, 1));
        await using var servicios = CrearProveedor();
        using var worker = CrearWorker(servicios);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            Assert.True(await EsperarAsync(async () => (await InstanciasDe(Hoy)).Count == 1), "No terminó la pasada inicial.");

            var generada = false;
            while (!generada && ZonaHogar.AhoraLocal(reloj.GetUtcNow()) < limite)
            {
                reloj.Advance(TimeSpan.FromMinutes(1));
                generada = await EsperarAsync(async () => (await InstanciasDe(Manana)).Any(i => i.DeberId == deber.Id));
            }

            Assert.True(generada, "La instancia del día nuevo no se generó a las 00:01 local.");
            Assert.True(ZonaHogar.AhoraLocal(reloj.GetUtcNow()) <= limite);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Worker_PasadaQueLanza_RegistraErrorYSigue()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Ocho, Repeticion.TodosLosDias, Hoy);
        var falla = new FallaEnLaPrimeraConsulta();
        await using var servicios = CrearProveedor(falla);
        using var worker = CrearWorker(servicios);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            Assert.True(await EsperarAsync(() => Task.FromResult(logger.Errores().Count == 1)), "La pasada que lanza no registró el error.");
            var error = Assert.Single(logger.Errores());
            Assert.Equal(EventoFallida, error.Evento.Id);
            Assert.Equal("GeneracionFallida", error.Evento.Name);
            Assert.Same(falla.Excepcion, error.Excepcion);
            Assert.Empty(await InstanciasDe(Hoy));

            reloj.Advance(TimeSpan.FromMinutes(1));

            Assert.True(await EsperarAsync(async () => (await InstanciasDe(Hoy)).Count == 1), "La segunda pasada no se ejecutó.");
            Assert.Equal(deber.Id, Assert.Single(await InstanciasDe(Hoy)).DeberId);
            Assert.Single(logger.Errores());
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Worker_InstanciaYaExistia_RegistraInformacionYSigue()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Ocho, Repeticion.TodosLosDias, Hoy);
        await using var servicios = CrearProveedor(
            new InsertaAntesDeGuardar(() => baseDeTest.InsertarInstancia(deber.Id, Hoy, Ocho)));
        using var worker = CrearWorker(servicios);

        await worker.EjecutarPasadaAsync(CancellationToken.None);
        await worker.EjecutarPasadaAsync(CancellationToken.None);

        var yaExistia = Assert.Single(logger.Entradas, e => e.Evento.Id == EventoYaExistia);
        Assert.Equal(LogLevel.Information, yaExistia.Nivel);
        Assert.Equal("InstanciaYaExistia", yaExistia.Evento.Name);
        Assert.Single(logger.Entradas, e => e.Evento.Id == EventoCompletada);
        Assert.Empty(logger.Errores());
        Assert.Single(await InstanciasDe(Hoy));
    }

    [Fact]
    public async Task Worker_Cancelacion_TerminaSinError()
    {
        baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Ocho, Repeticion.TodosLosDias, Hoy);
        await using var servicios = CrearProveedor();
        using var worker = CrearWorker(servicios);

        await worker.StartAsync(CancellationToken.None);
        Assert.True(await EsperarAsync(() => Task.FromResult(logger.Pasadas() >= 1)), "No terminó la pasada inicial.");

        await worker.StopAsync(CancellationToken.None);

        var ejecucion = Assert.IsAssignableFrom<Task>(worker.ExecuteTask);
        Assert.True(ejecucion.IsCompletedSuccessfully, $"ExecuteAsync terminó con estado {ejecucion.Status}.");
        Assert.Empty(logger.Errores());
    }

    private ServiceProvider CrearProveedor(params IInterceptor[] interceptores)
    {
        if (proveedor is not null)
        {
            throw new InvalidOperationException("Un proveedor por test.");
        }

        var services = new ServiceCollection();
        services.AddDbContext<PetCareDbContext>(o => o.UseSqlite(baseDeTest.Conexion).AddInterceptors(interceptores));
        services.AddSingleton<TimeProvider>(reloj);
        services.AddSingleton<GeneracionLock>();
        services.AddScoped<InstanciasService>();
        proveedor = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        return proveedor;
    }

    private GeneracionDiariaWorker CrearWorker(ServiceProvider servicios) =>
        new(servicios.GetRequiredService<IServiceScopeFactory>(), reloj, logger);

    /// <summary>
    /// Lee bajo el mismo <see cref="GeneracionLock"/> que toma el servicio: la base en memoria es
    /// una única <c>SqliteConnection</c>, que no admite uso simultáneo desde el hilo del worker y
    /// el del test.
    /// </summary>
    private async Task<List<Instancia>> InstanciasDe(DateOnly fecha)
    {
        var generacionLock = proveedor?.GetRequiredService<GeneracionLock>()
            ?? throw new InvalidOperationException("Falta crear el proveedor.");
        using (await generacionLock.TomarAsync(CancellationToken.None))
        {
            await using var contexto = baseDeTest.CrearContexto();
            return await contexto.Instancias.Where(i => i.Fecha == fecha).ToListAsync();
        }
    }

    /// <summary>Espera, en tiempo real, a que el worker (que corre en otro hilo) cumpla la condición.</summary>
    private static async Task<bool> EsperarAsync(Func<Task<bool>> condicion)
    {
        var cronometro = Stopwatch.StartNew();
        while (cronometro.Elapsed < EsperaMaxima)
        {
            if (await condicion())
            {
                return true;
            }

            await Task.Delay(10);
        }

        return await condicion();
    }

    private sealed record EntradaDeLog(LogLevel Nivel, EventId Evento, Exception? Excepcion, string Mensaje);

    private sealed class LoggerDePrueba : ILogger<GeneracionDiariaWorker>
    {
        private readonly ConcurrentQueue<EntradaDeLog> entradas = new();

        public IReadOnlyList<EntradaDeLog> Entradas => entradas.ToArray();

        public List<EntradaDeLog> Errores() => Entradas.Where(e => e.Nivel >= LogLevel.Error).ToList();

        /// <summary>Pasadas terminadas: cada una deja exactamente uno de los eventos 3000, 3001 o 3002.</summary>
        public int Pasadas() => Entradas.Count(e => e.Evento.Id is EventoCompletada or EventoYaExistia or EventoFallida);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            entradas.Enqueue(new EntradaDeLog(logLevel, eventId, exception, formatter(state, exception)));
    }

    /// <summary>Hace que la primera pasada lance al consultar la base; las siguientes funcionan.</summary>
    private sealed class FallaEnLaPrimeraConsulta : DbCommandInterceptor
    {
        private int consultas;

        public InvalidOperationException Excepcion { get; } = new("falla-de-prueba-en-la-consulta");

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default) =>
            Interlocked.Increment(ref consultas) == 1 ? throw Excepcion : ValueTask.FromResult(result);
    }
}
