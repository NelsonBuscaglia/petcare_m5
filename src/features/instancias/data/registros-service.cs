using Microsoft.EntityFrameworkCore;
using PetCare.Features.Instancias.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Resultados;
using PetCare.Shared.Tiempo;

namespace PetCare.Features.Instancias.Data;

/// <summary>
/// Único punto de escritura de <see cref="RegistroCumplimiento"/> (alta y eliminación). Los errores de base que no son
/// reglas de negocio no se capturan: llegan al manejador global.
/// </summary>
public sealed class RegistrosService(PetCareDbContext db, TimeProvider time)
{
    /// <summary>
    /// Registra el cumplimiento de una instancia vigente (FR-02) con la hora real ingresada
    /// (FR-03, FR-04) y la fecha de hoy (FR-05). Devuelve el id del registro.
    /// </summary>
    public async Task<Resultado<int, CumplimientoError>> RegistrarAsync(
        int instanciaId, string? horaReal, CancellationToken cancellationToken)
    {
        var instancia = await LeerAsync(instanciaId, cancellationToken);
        if (instancia is null)
        {
            return Resultado<int, CumplimientoError>.Falla([CumplimientoError.InstanciaNoEncontrada]);
        }

        var ahoraLocal = ZonaHogar.AhoraLocal(time.GetUtcNow());
        var validacion = ValidacionCumplimiento.Validar(horaReal, instancia.Fecha, instancia.TieneRegistro, ahoraLocal);
        if (!validacion.EsOk)
        {
            return Resultado<int, CumplimientoError>.Falla(validacion.Errores);
        }

        var registro = new RegistroCumplimiento
        {
            InstanciaId = instanciaId,
            Fecha = DateOnly.FromDateTime(ahoraLocal),
            HoraReal = validacion.Valor,
        };
        db.RegistrosCumplimiento.Add(registro);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqliteErrores.EsViolacionUnica(ex))
        {
            // El índice único de InstanciaId rechazó el registro: otro cuidador cumplió la misma
            // instancia entre la lectura y el guardado (FR-06). Se descarta el pendiente para que
            // el contexto no lo reintente.
            db.ChangeTracker.Clear();
            return Resultado<int, CumplimientoError>.Falla([CumplimientoError.InstanciaYaCumplida]);
        }

        return Resultado<int, CumplimientoError>.Ok(registro.Id);
    }

    /// <summary>
    /// Hora a proponer en el formulario de cumplimiento de una instancia vigente: la actual,
    /// truncada al minuto (FR-03).
    /// </summary>
    public async Task<Resultado<TimeOnly, CumplimientoError>> PrepararFormularioAsync(
        int instanciaId, CancellationToken cancellationToken)
    {
        var instancia = await LeerAsync(instanciaId, cancellationToken);
        if (instancia is null)
        {
            return Resultado<TimeOnly, CumplimientoError>.Falla([CumplimientoError.InstanciaNoEncontrada]);
        }

        var ahoraLocal = ZonaHogar.AhoraLocal(time.GetUtcNow());
        if (ValidacionCumplimiento.ErrorDeVigencia(instancia.Fecha, instancia.TieneRegistro, ahoraLocal) is { } error)
        {
            return Resultado<TimeOnly, CumplimientoError>.Falla([error]);
        }

        return Resultado<TimeOnly, CumplimientoError>.Ok(new TimeOnly(ahoraLocal.Hour, ahoraLocal.Minute));
    }

    /// <summary>
    /// Elimina un registro de hoy (FR-09, FR-10) y deja vigente su instancia, marcando cuándo se
    /// reactivó (FR-11). Devuelve el id de la instancia. Un id que no existe —nunca existió o ya
    /// se eliminó— es <see cref="CumplimientoError.RegistroYaEliminado"/> (FR-12).
    /// </summary>
    public async Task<Resultado<int, CumplimientoError>> EliminarAsync(int registroId, CancellationToken cancellationToken)
    {
        var ahoraUtc = time.GetUtcNow();
        var registro = await db.RegistrosCumplimiento
            .Where(r => r.Id == registroId)
            .Select(r => new RegistroParaEliminar(r.Fecha, r.InstanciaId))
            .SingleOrDefaultAsync(cancellationToken);
        if (registro is null)
        {
            return Resultado<int, CumplimientoError>.Falla([CumplimientoError.RegistroYaEliminado]);
        }

        if (registro.Fecha != ZonaHogar.Hoy(ahoraUtc))
        {
            return Resultado<int, CumplimientoError>.Falla([CumplimientoError.RegistroDeOtroDia]);
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);
        var borrados = await db.RegistrosCumplimiento
            .Where(r => r.Id == registroId)
            .ExecuteDeleteAsync(cancellationToken);
        if (borrados == 0)
        {
            // Otro cuidador lo eliminó entre la lectura y el borrado: no se reactiva dos veces.
            await transaccion.RollbackAsync(cancellationToken);
            return Resultado<int, CumplimientoError>.Falla([CumplimientoError.RegistroYaEliminado]);
        }

        DateTimeOffset? reactivadaEnUtc = ahoraUtc.ToUniversalTime();
        await db.Instancias
            .Where(i => i.Id == registro.InstanciaId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.ReactivadaEnUtc, reactivadaEnUtc), cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
        return Resultado<int, CumplimientoError>.Ok(registro.InstanciaId);
    }

    private async Task<InstanciaParaCumplir?> LeerAsync(int instanciaId, CancellationToken cancellationToken) =>
        await db.Instancias
            .Where(i => i.Id == instanciaId)
            .Select(i => new InstanciaParaCumplir(i.Fecha, db.RegistrosCumplimiento.Any(r => r.InstanciaId == i.Id)))
            .SingleOrDefaultAsync(cancellationToken);

    private sealed record InstanciaParaCumplir(DateOnly Fecha, bool TieneRegistro);

    private sealed record RegistroParaEliminar(DateOnly Fecha, int InstanciaId);
}
