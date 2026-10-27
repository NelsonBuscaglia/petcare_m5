using PetCare.Features.Emails.Data;
using PetCare.Features.Emails.Domain;

namespace PetCare.Shared.Hosting;

/// <summary>
/// Revisa los vencimientos al arrancar y después cada 1 minuto (FR-13, FR-16, NFR-01), sin
/// depender de que haya cuidadores con la app abierta. No tiene lógica propia: cada pasada delega
/// en <see cref="EmailsVencimientoService"/>. Sus logs nunca llevan direcciones de email ni
/// <c>Exception.Message</c> (M3 reformulada).
/// </summary>
public sealed partial class VencimientosWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider time,
    ILogger<VencimientosWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // El timer se crea antes de la pasada inicial para que el primer tick no dependa de cuánto
        // tarde esa pasada (un intento puede tardar hasta 30 s).
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
            var servicio = scope.ServiceProvider.GetRequiredService<EmailsVencimientoService>();
            var resumen = await servicio.ProcesarAsync(cancellationToken);
            foreach (var aceptado in resumen.Aceptados)
            {
                LogEmailAceptado(aceptado.InstanciaId, aceptado.YaRegistrado);
            }

            foreach (var fallido in resumen.Fallidos)
            {
                LogEnvioFallido(fallido.InstanciaId, fallido.Error);
            }

            LogPasada(resumen.Aceptados.Count, resumen.Fallidos.Count);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            // Solo el tipo y el HResult: el mensaje de una excepción de MailKit o de la base puede
            // traer direcciones de email (M3 reformulada). Se reintenta en el próximo tick.
            LogPasadaFallida(ex.GetType().FullName ?? ex.GetType().Name, ex.HResult);
        }
    }

    // Sin envíos (lo habitual, una vez por minuto) queda en Debug para no ensuciar el log.
    private void LogPasada(int aceptados, int fallidos) =>
        LogPasada(aceptados + fallidos > 0 ? LogLevel.Information : LogLevel.Debug, aceptados, fallidos);

    [LoggerMessage(EventId = 4000, EventName = "PasadaDeVencimientos",
        Message = "Vencimientos: {Aceptados} emails aceptados, {Fallidos} fallidos.")]
    private partial void LogPasada(LogLevel nivel, int aceptados, int fallidos);

    [LoggerMessage(EventId = 4001, EventName = "EmailVencimientoAceptado", Level = LogLevel.Information,
        Message = "Email de vencimiento aceptado para la instancia {InstanciaId} (ya estaba registrado: {YaRegistrado}).")]
    private partial void LogEmailAceptado(int instanciaId, bool yaRegistrado);

    [LoggerMessage(EventId = 4002, EventName = "EnvioFallido", Level = LogLevel.Warning,
        Message = "Email de vencimiento de la instancia {InstanciaId} no aceptado ({Error}); se reintenta en la próxima pasada si sigue en ventana.")]
    private partial void LogEnvioFallido(int instanciaId, EnvioEmailError error);

    [LoggerMessage(EventId = 4003, EventName = "PasadaDeVencimientosFallida", Level = LogLevel.Error,
        Message = "Pasada de vencimientos fallida ({TipoExcepcion}, HResult {HResult}); se reintenta en la próxima pasada.")]
    private partial void LogPasadaFallida(string tipoExcepcion, int hResult);
}
