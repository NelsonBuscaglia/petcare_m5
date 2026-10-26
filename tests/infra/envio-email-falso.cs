using System.Collections.Concurrent;
using PetCare.Features.Emails.Domain;
using PetCare.Shared.Resultados;

namespace PetCare.Tests.Infra;

public enum ResultadoDelIntento
{
    Aceptado,
    Rechazado,
    SinRespuesta,
    Lanzo,
}

/// <summary>Un intento de envío visto por el falso, con el instante (del reloj inyectado) en que empezó.</summary>
public sealed record IntentoDeEmail(DateTimeOffset Instante, MensajeEmail Mensaje, ResultadoDelIntento Resultado);

/// <summary>
/// <see cref="IEnvioEmail"/> de prueba: registra cada intento y se programa para aceptar (por
/// defecto), rechazar hasta un instante, no responder hasta un instante (espera la cancelación del
/// token, como un servidor colgado) o lanzar una excepción en el próximo intento.
/// </summary>
public sealed class EnvioEmailFalso(TimeProvider reloj) : IEnvioEmail
{
    private readonly ConcurrentQueue<IntentoDeEmail> intentos = new();
    private readonly Lock candado = new();
    private DateTimeOffset rechazarHasta = DateTimeOffset.MinValue;
    private DateTimeOffset noResponderHasta = DateTimeOffset.MinValue;
    private Exception? lanzar;

    public IReadOnlyList<IntentoDeEmail> Intentos => intentos.ToArray();

    public IReadOnlyList<IntentoDeEmail> Aceptados =>
        Intentos.Where(i => i.Resultado == ResultadoDelIntento.Aceptado).ToList();

    /// <summary>Rechaza todo intento que empiece antes de <paramref name="instanteUtc"/>.</summary>
    public void RechazarHasta(DateTimeOffset instanteUtc)
    {
        lock (candado)
        {
            rechazarHasta = instanteUtc;
        }
    }

    /// <summary>No responde a ningún intento que empiece antes de <paramref name="instanteUtc"/>.</summary>
    public void NoResponderHasta(DateTimeOffset instanteUtc)
    {
        lock (candado)
        {
            noResponderHasta = instanteUtc;
        }
    }

    public void LanzarEnElProximoIntento(Exception excepcion)
    {
        lock (candado)
        {
            lanzar = excepcion;
        }
    }

    public async Task<Resultado<bool, EnvioEmailError>> EnviarAsync(MensajeEmail mensaje, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        Exception? aLanzar;
        bool rechaza;
        bool noResponde;
        lock (candado)
        {
            aLanzar = lanzar;
            lanzar = null;
            rechaza = ahora < rechazarHasta;
            noResponde = ahora < noResponderHasta;
        }

        if (aLanzar is not null)
        {
            intentos.Enqueue(new IntentoDeEmail(ahora, mensaje, ResultadoDelIntento.Lanzo));
            throw aLanzar;
        }

        if (rechaza)
        {
            intentos.Enqueue(new IntentoDeEmail(ahora, mensaje, ResultadoDelIntento.Rechazado));
            return Resultado<bool, EnvioEmailError>.Falla([EnvioEmailError.Rechazado]);
        }

        if (noResponde)
        {
            // Se registra antes de colgarse para que el test vea el intento en curso.
            intentos.Enqueue(new IntentoDeEmail(ahora, mensaje, ResultadoDelIntento.SinRespuesta));
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            throw new InvalidOperationException("Task.Delay infinito terminó sin cancelación.");
        }

        intentos.Enqueue(new IntentoDeEmail(ahora, mensaje, ResultadoDelIntento.Aceptado));
        return Resultado<bool, EnvioEmailError>.Ok(true);
    }
}
