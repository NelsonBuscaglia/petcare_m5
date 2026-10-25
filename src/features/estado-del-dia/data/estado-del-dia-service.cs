using Microsoft.EntityFrameworkCore;
using PetCare.Shared.Data;
using PetCare.Shared.Tiempo;

namespace PetCare.Features.EstadoDelDia.Data;

// Dentro del namespace: si no, "EstadoDelDia" se resolvería al namespace PetCare.Features.EstadoDelDia.
using PetCare.Features.EstadoDelDia.Domain;
using PetCare.Features.Hogar.Domain;

/// <summary>
/// Lee el estado del día directamente del <see cref="PetCareDbContext"/> compartido (tablas de
/// hogar, deberes e instancias: decisión transversal del spec) y lo arma con la función pura.
/// Los errores de base no se capturan: llegan al manejador global.
/// </summary>
public sealed class EstadoDelDiaService(PetCareDbContext db, TimeProvider time)
{
    public async Task<EstadoDelDia> ObtenerAsync(CancellationToken cancellationToken)
    {
        var ahora = ZonaHogar.AhoraLocal(time.GetUtcNow());
        var hoy = DateOnly.FromDateTime(ahora);

        var mascotas = await db.Mascotas
            .OrderBy(m => m.Nombre)
            .Select(m => new MascotaVista(m.Id, m.Nombre))
            .ToListAsync(cancellationToken);

        var instancias = await (
                from i in db.Instancias
                join d in db.Deberes on i.DeberId equals d.Id
                where i.Fecha == hoy
                select new InstanciaLeida(
                    i.Id,
                    d.MascotaId,
                    d.Tipo,
                    i.Fecha,
                    i.Hora,
                    db.RegistrosCumplimiento.Any(r => r.InstanciaId == i.Id)))
            .ToListAsync(cancellationToken);

        var registros = await LeerRegistrosAsync(hoy, cancellationToken);

        return ArmadoEstadoDelDia.Armar(mascotas, instancias, registros, ahora);
    }

    /// <summary>
    /// Registros acotados: los de hoy (lista y paseos del día) más el último por mascota y tipo
    /// (resumen), en dos lecturas. Pueden repetirse ids entre ambas: <see cref="ArmadoEstadoDelDia"/>
    /// deduplica. <c>DateOnly</c>/<c>TimeOnly</c> se guardan como TEXT ISO, así que ordenarlos en
    /// SQL es cronológico.
    /// </summary>
    internal async Task<IReadOnlyList<RegistroLeido>> LeerRegistrosAsync(DateOnly hoy, CancellationToken cancellationToken)
    {
        var registros =
            from r in db.RegistrosCumplimiento
            join i in db.Instancias on r.InstanciaId equals i.Id
            join d in db.Deberes on i.DeberId equals d.Id
            select new { r.Id, d.MascotaId, d.Tipo, r.Fecha, r.HoraReal };

        var deHoy = await registros
            .Where(x => x.Fecha == hoy)
            .Select(x => new RegistroLeido(x.Id, x.MascotaId, x.Tipo, x.Fecha, x.HoraReal))
            .ToListAsync(cancellationToken);

        // Sin proyectar después del First(): EF Core no traduce un Select sobre el resultado del
        // grupo; se materializa la fila tal cual (ya filtrada en SQL) y se convierte en memoria.
        var ultimos = await registros
            .GroupBy(x => new { x.MascotaId, x.Tipo })
            .Select(g => g.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.HoraReal).First())
            .ToListAsync(cancellationToken);

        return [.. deHoy, .. ultimos.Select(x => new RegistroLeido(x.Id, x.MascotaId, x.Tipo, x.Fecha, x.HoraReal))];
    }
}
