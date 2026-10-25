using PetCare.Features.Instancias.Data;

namespace PetCare.Shared.Hosting;

/// <summary>
/// Genera las instancias del día al arrancar (FR-09) y después cada 1 minuto (FR-08, NFR-01).
/// No tiene lógica propia: cada pasada delega en <see cref="InstanciasService"/>.
/// </summary>
public sealed partial class GeneracionDiariaWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider time,
    ILogger<GeneracionDiariaWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // El timer se crea antes de la pasada inicial para que el primer tick no dependa de cuánto
        // tarde esa pasada.
        using var timer = new PeriodicTimer(Intervalo, time);
        try
        {
            await EjecutarPasadaAsync(stoppingToken);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await EjecutarPasadaAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Cancelación del host: fin normal del loop, no es un error.
        }
    }

    internal async Task EjecutarPasadaAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var servicio = scope.ServiceProvider.GetRequiredService<InstanciasService>();
            var resultado = await servicio.GenerarDelDiaAsync(cancellationToken);
            if (resultado.EsOk)
            {
                LogGeneracionCompletada(resultado.Valor);
            }
            else
            {
                LogInstanciaYaExistia();
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            LogGeneracionFallida(ex);
        }
    }

    // Con 0 instancias nuevas (lo habitual, una vez por minuto) queda en Debug para no ensuciar el log.
    private void LogGeneracionCompletada(int cantidad) =>
        LogGeneracionCompletada(cantidad > 0 ? LogLevel.Information : LogLevel.Debug, cantidad);

    [LoggerMessage(EventId = 3000, EventName = "GeneracionCompletada",
        Message = "Generación diaria: {Cantidad} instancias creadas.")]
    private partial void LogGeneracionCompletada(LogLevel nivel, int cantidad);

    [LoggerMessage(EventId = 3001, EventName = "InstanciaYaExistia", Level = LogLevel.Information,
        Message = "Generación diaria: otra operación ya había creado una instancia; se reintenta en la próxima pasada.")]
    private partial void LogInstanciaYaExistia();

    [LoggerMessage(EventId = 3002, EventName = "GeneracionFallida", Level = LogLevel.Error,
        Message = "Generación diaria fallida; se reintenta en la próxima pasada.")]
    private partial void LogGeneracionFallida(Exception ex);
}
