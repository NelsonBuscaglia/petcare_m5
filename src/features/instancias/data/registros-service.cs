using Microsoft.EntityFrameworkCore;
using PetCare.Features.Instancias.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Resultados;
using PetCare.Shared.Tiempo;

namespace PetCare.Features.Instancias.Data;

/// <summary>
/// Único punto de escritura de <see cref="RegistroCumplimiento"/>. Los errores de base que no son
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

    private async Task<InstanciaParaCumplir?> LeerAsync(int instanciaId, CancellationToken cancellationToken) =>
        await db.Instancias
            .Where(i => i.Id == instanciaId)
            .Select(i => new InstanciaParaCumplir(i.Fecha, db.RegistrosCumplimiento.Any(r => r.InstanciaId == i.Id)))
            .SingleOrDefaultAsync(cancellationToken);

    private sealed record InstanciaParaCumplir(DateOnly Fecha, bool TieneRegistro);
}
