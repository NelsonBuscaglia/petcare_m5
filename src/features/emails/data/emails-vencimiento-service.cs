using Microsoft.EntityFrameworkCore;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Emails.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Resultados;
using PetCare.Shared.Tiempo;

namespace PetCare.Features.Emails.Data;

/// <summary>Email aceptado por el servicio. <c>YaRegistrado</c>: otra pasada lo había guardado antes (índice único).</summary>
public sealed record EnvioAceptado(int InstanciaId, bool YaRegistrado);

public sealed record EnvioFallido(int InstanciaId, EnvioEmailError Error);

public sealed record ResumenEnvios(IReadOnlyList<EnvioAceptado> Aceptados, IReadOnlyList<EnvioFallido> Fallidos);

/// <summary>
/// Una pasada de emails de vencimiento (FR-13..FR-18). Único punto de escritura de
/// <see cref="EmailVencimiento"/>. Las excepciones que no son fallas del servicio de envío ni el
/// choque del índice único se propagan (las registra el worker).
/// </summary>
public sealed class EmailsVencimientoService(PetCareDbContext db, IEnvioEmail envio, TimeProvider time)
{
    /// <summary>Un intento sin respuesta en este plazo se da por fallido (FR-18).</summary>
    public static readonly TimeSpan TimeoutPorIntento = TimeSpan.FromSeconds(30);

    public async Task<ResumenEnvios> ProcesarAsync(CancellationToken cancellationToken)
    {
        var hoy = ZonaHogar.Hoy(time.GetUtcNow());

        // Sin filtros sobre DateTimeOffset en SQL (se guarda como TEXT): la condición se evalúa en memoria.
        var instancias = await (
                from i in db.Instancias
                join d in db.Deberes on i.DeberId equals d.Id
                join m in db.Mascotas on d.MascotaId equals m.Id
                where i.Fecha == hoy
                orderby i.Id
                select new InstanciaLeida(
                    i.Id,
                    i.Fecha,
                    i.Hora,
                    db.RegistrosCumplimiento.Any(r => r.InstanciaId == i.Id),
                    db.EmailsVencimiento.Any(e => e.InstanciaId == i.Id),
                    i.VencidaAlGenerarse,
                    i.ReactivadaEnUtc,
                    d.Tipo,
                    m.Nombre,
                    m.HogarId))
            .ToListAsync(cancellationToken);

        var candidatas = instancias
            .Select(i => (Leida: i, Candidata: i.ComoCandidata()))
            .Where(x => CondicionEmail.DebeIntentarse(x.Candidata, time.GetUtcNow()))
            .ToList();
        if (candidatas.Count == 0)
        {
            return new ResumenEnvios([], []);
        }

        var hogares = candidatas.Select(x => x.Leida.HogarId).Distinct().ToList();
        var cuidadores = (await db.Cuidadores
                .Where(c => hogares.Contains(c.HogarId))
                .OrderBy(c => c.Id)
                .Select(c => new { c.HogarId, c.Email })
                .ToListAsync(cancellationToken))
            .ToLookup(c => c.HogarId, c => c.Email);

        var aceptados = new List<EnvioAceptado>();
        var fallidos = new List<EnvioFallido>();
        foreach (var (leida, candidata) in candidatas)
        {
            // Un intento anterior pudo tardar hasta 30 s: la ventana se vuelve a mirar con la hora actual.
            if (!CondicionEmail.DebeIntentarse(candidata, time.GetUtcNow()))
            {
                continue;
            }

            var mensaje = MensajeVencimiento.Armar(leida.NombreMascota, leida.Tipo, leida.Fecha, leida.Hora, [.. cuidadores[leida.HogarId]]);
            var resultado = await IntentarAsync(mensaje, cancellationToken);
            if (resultado.EsOk)
            {
                aceptados.Add(await RegistrarAceptadoAsync(leida.Id, cancellationToken));
            }
            else
            {
                fallidos.Add(new EnvioFallido(leida.Id, resultado.Errores[0]));
            }
        }

        return new ResumenEnvios(aceptados, fallidos);
    }

    private async Task<Resultado<bool, EnvioEmailError>> IntentarAsync(MensajeEmail mensaje, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeoutPorIntento, time);
        using var enlazado = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            return await envio.EnviarAsync(mensaje, enlazado.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Venció el plazo del intento, no se está apagando el host: el servicio no respondió (FR-18).
            return Resultado<bool, EnvioEmailError>.Falla([EnvioEmailError.SinRespuesta]);
        }
    }

    private async Task<EnvioAceptado> RegistrarAceptadoAsync(int instanciaId, CancellationToken cancellationToken)
    {
        db.EmailsVencimiento.Add(new EmailVencimiento
        {
            InstanciaId = instanciaId,
            AceptadoEnUtc = time.GetUtcNow().ToUniversalTime(),
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new EnvioAceptado(instanciaId, YaRegistrado: false);
        }
        catch (DbUpdateException ex) when (SqliteErrores.EsViolacionUnica(ex))
        {
            // Otra pasada ya había registrado el email de esta instancia: no se reintenta. Se descarta
            // el pendiente para que el contexto no lo vuelva a guardar.
            db.ChangeTracker.Clear();
            return new EnvioAceptado(instanciaId, YaRegistrado: true);
        }
    }

    private sealed record InstanciaLeida(
        int Id,
        DateOnly Fecha,
        TimeOnly Hora,
        bool TieneRegistro,
        bool TieneEmailAceptado,
        bool VencidaAlGenerarse,
        DateTimeOffset? ReactivadaEnUtc,
        TipoDeber Tipo,
        string NombreMascota,
        int HogarId)
    {
        public CandidataEmail ComoCandidata() =>
            new(Id, Fecha, Hora, TieneRegistro, TieneEmailAceptado, VencidaAlGenerarse, ReactivadaEnUtc);
    }
}
