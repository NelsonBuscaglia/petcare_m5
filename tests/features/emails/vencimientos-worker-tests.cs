using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Emails.Data;
using PetCare.Features.Emails.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Hosting;
using PetCare.Shared.Tiempo;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.Emails;

public sealed class VencimientosWorkerTests : IDisposable
{
    private const int Firulais = 1;
    private const int EventoPasada = 4000;
    private const int EventoAceptado = 4001;
    private const int EventoEnvioFallido = 4002;
    private const int EventoPasadaFallida = 4003;

    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly TimeOnly Nueve = new(9, 0);
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(5);

    private readonly BaseDeTest baseDeTest = BaseDeTest.Crear();
    private readonly FakeTimeProvider reloj = new(Local(9, 0));
    private readonly LoggerDePrueba logger = new();
    private readonly EnvioEmailFalso envio;

    public VencimientosWorkerTests()
    {
        envio = new EnvioEmailFalso(reloj);
    }

    public void Dispose() => baseDeTest.Dispose();

    [Fact]
    public async Task VencimientosWorker_SinAppAbierta_EnviaAntesDe0920()
    {
        var instancia = InsertarComida0900();
        await using var servicios = CrearProveedor();
        using var worker = CrearWorker(servicios);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            Assert.True(await EsperarAsync(() => logger.Pasadas() >= 1), "No terminó la pasada inicial.");
            while (envio.Aceptados.Count == 0 && reloj.GetUtcNow() < Local(9, 20))
            {
                var pasadas = logger.Pasadas();
                reloj.Advance(TimeSpan.FromMinutes(1));
                Assert.True(await EsperarAsync(() => logger.Pasadas() > pasadas), "El tick no ejecutó una pasada.");
            }

            var aceptado = Assert.Single(envio.Aceptados);
            Assert.InRange(aceptado.Instante, Local(9, 15), Local(9, 20));
            Assert.Equal(["ana@ejemplo.com", "beto@ejemplo.com"], aceptado.Mensaje.Para);
            var evento = Assert.Single(logger.Entradas, e => e.Evento.Id == EventoAceptado);
            Assert.Equal(LogLevel.Information, evento.Nivel);
            Assert.Contains(instancia.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), evento.Mensaje);
            Assert.Empty(logger.Errores());
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }

        Assert.Single(await Emails());
    }

    [Fact]
    public async Task VencimientosWorker_EnvioFallido_RegistraWarningSinDirecciones()
    {
        var instancia = InsertarComida0900();
        reloj.SetUtcNow(Local(9, 15));
        envio.RechazarHasta(Local(9, 30));
        await using var servicios = CrearProveedor();
        using var worker = CrearWorker(servicios);

        await worker.EjecutarPasadaAsync(CancellationToken.None);

        var fallido = Assert.Single(logger.Entradas, e => e.Evento.Id == EventoEnvioFallido);
        Assert.Equal(LogLevel.Warning, fallido.Nivel);
        Assert.Equal("EnvioFallido", fallido.Evento.Name);
        Assert.Contains(instancia.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), fallido.Mensaje);
        Assert.Contains(nameof(EnvioEmailError.Rechazado), fallido.Mensaje);
        Assert.Null(fallido.Excepcion);
        Assert.Contains(logger.Entradas, e => e.Evento.Id == EventoPasada);
        Assert.All(logger.Entradas, e => Assert.DoesNotContain("@", e.Mensaje));
        Assert.Single(envio.Intentos);
    }

    [Fact]
    public async Task VencimientosWorker_PasadaQueLanza_RegistraTipoYSigue()
    {
        InsertarComida0900();
        reloj.SetUtcNow(Local(9, 15));
        envio.LanzarEnElProximoIntento(new InvalidOperationException("detalle-secreto ana@ejemplo.com"));
        await using var servicios = CrearProveedor();
        using var worker = CrearWorker(servicios);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            Assert.True(await EsperarAsync(() => logger.Errores().Count == 1), "La pasada que lanza no registró el error.");
            var error = Assert.Single(logger.Errores());
            Assert.Equal(EventoPasadaFallida, error.Evento.Id);
            Assert.Equal("PasadaDeVencimientosFallida", error.Evento.Name);
            Assert.Contains(nameof(InvalidOperationException), error.Mensaje);
            Assert.DoesNotContain("detalle-secreto", error.Mensaje);
            Assert.Null(error.Excepcion);

            reloj.Advance(TimeSpan.FromMinutes(1));

            Assert.True(await EsperarAsync(() => envio.Aceptados.Count == 1), "La pasada siguiente no corrió.");
            Assert.True(await EsperarAsync(() => logger.Entradas.Any(e => e.Evento.Id == EventoAceptado)));
            Assert.Single(logger.Errores());
            Assert.All(logger.Entradas, e => Assert.DoesNotContain("@", e.Mensaje));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task VencimientosWorker_Cancelacion_TerminaSinError()
    {
        InsertarComida0900();
        await using var servicios = CrearProveedor();
        using var worker = CrearWorker(servicios);

        await worker.StartAsync(CancellationToken.None);
        Assert.True(await EsperarAsync(() => logger.Pasadas() >= 1), "No terminó la pasada inicial.");

        await worker.StopAsync(CancellationToken.None);

        var ejecucion = Assert.IsAssignableFrom<Task>(worker.ExecuteTask);
        Assert.True(ejecucion.IsCompletedSuccessfully, $"ExecuteAsync terminó con estado {ejecucion.Status}.");
        Assert.Empty(logger.Errores());
        var pasada = Assert.Single(logger.Entradas, e => e.Evento.Id == EventoPasada);
        Assert.Equal(LogLevel.Debug, pasada.Nivel);
    }

    private PetCare.Features.Instancias.Domain.Instancia InsertarComida0900()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Nueve, Repeticion.TodosLosDias, Hoy);
        return baseDeTest.InsertarInstancia(deber.Id, Hoy, Nueve);
    }

    private ServiceProvider CrearProveedor()
    {
        var services = new ServiceCollection();
        services.AddDbContext<PetCareDbContext>(o => o.UseSqlite(baseDeTest.Conexion));
        services.AddSingleton<TimeProvider>(reloj);
        services.AddSingleton<IEnvioEmail>(envio);
        services.AddScoped<EmailsVencimientoService>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private VencimientosWorker CrearWorker(ServiceProvider servicios) =>
        new(servicios.GetRequiredService<IServiceScopeFactory>(), reloj, logger);

    /// <summary>Se llama con el worker detenido: la conexión en memoria no admite uso simultáneo.</summary>
    private async Task<List<EmailVencimiento>> Emails()
    {
        await using var contexto = baseDeTest.CrearContexto();
        return await contexto.EmailsVencimiento.ToListAsync();
    }

    private static DateTimeOffset Local(int hora, int minuto) =>
        ZonaHogar.AUtc(Hoy.ToDateTime(new TimeOnly(hora, minuto)));

    /// <summary>Espera, en tiempo real, a que el worker (que corre en otro hilo) cumpla la condición.</summary>
    private static async Task<bool> EsperarAsync(Func<bool> condicion)
    {
        var cronometro = Stopwatch.StartNew();
        while (cronometro.Elapsed < EsperaMaxima)
        {
            if (condicion())
            {
                return true;
            }

            await Task.Delay(10);
        }

        return condicion();
    }

    private sealed record EntradaDeLog(LogLevel Nivel, EventId Evento, Exception? Excepcion, string Mensaje);

    private sealed class LoggerDePrueba : ILogger<VencimientosWorker>
    {
        private readonly ConcurrentQueue<EntradaDeLog> entradas = new();

        public IReadOnlyList<EntradaDeLog> Entradas => entradas.ToArray();

        public List<EntradaDeLog> Errores() => Entradas.Where(e => e.Nivel >= LogLevel.Error).ToList();

        /// <summary>Pasadas terminadas: cada una deja exactamente uno de los eventos 4000 o 4003.</summary>
        public int Pasadas() => Entradas.Count(e => e.Evento.Id is EventoPasada or EventoPasadaFallida);

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
}
