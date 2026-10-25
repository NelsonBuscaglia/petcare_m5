using Microsoft.EntityFrameworkCore;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Instancias.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Hosting;
using PetCare.Shared.Resultados;
using PetCare.Shared.Tiempo;

namespace PetCare.Features.Instancias.Data;

public sealed class InstanciasService(PetCareDbContext db, TimeProvider time, GeneracionLock generacionLock)
{
    /// <summary>
    /// Genera las instancias de hoy que falten (FR-08, FR-09) y devuelve cuántas creó. Idempotente:
    /// una segunda ejecución el mismo día crea 0. Solo genera el día actual.
    /// </summary>
    public async Task<Resultado<int, GeneracionError>> GenerarDelDiaAsync(CancellationToken cancellationToken)
    {
        using (await generacionLock.TomarAsync(cancellationToken))
        {
            var ahoraLocal = ZonaHogar.AhoraLocal(time.GetUtcNow());
            var hoy = DateOnly.FromDateTime(ahoraLocal);
            var deberes = await db.Deberes
                .Where(d => d.Repeticion == Repeticion.TodosLosDias)
                .Select(d => new DeberParaGenerar(d.Id, d.Hora, d.Repeticion, d.FechaAlta))
                .ToListAsync(cancellationToken);
            var conInstanciaHoy = await db.Instancias
                .Where(i => i.Fecha == hoy)
                .Select(i => i.DeberId)
                .ToListAsync(cancellationToken);

            var faltan = GeneracionInstancias.QueFaltan(hoy, deberes, conInstanciaHoy.ToHashSet());
            if (faltan.Count == 0)
            {
                return Resultado<int, GeneracionError>.Ok(0);
            }

            db.Instancias.AddRange(faltan.Select(f => new Instancia
            {
                DeberId = f.DeberId,
                Fecha = f.Fecha,
                Hora = f.Hora,
                VencidaAlGenerarse = EstadoInstancia.EsVencida(f.Fecha, f.Hora, ahoraLocal),
            }));
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (SqliteErrores.EsViolacionUnica(ex))
            {
                // El índice único (DeberId, Fecha) rechazó la inserción: otra operación creó alguna
                // de estas instancias entre la lectura y el guardado. Se descartan las pendientes
                // para que el contexto no las reintente; la próxima pasada genera las que sigan
                // faltando.
                db.ChangeTracker.Clear();
                return Resultado<int, GeneracionError>.Falla([GeneracionError.InstanciaYaExistia]);
            }

            return Resultado<int, GeneracionError>.Ok(faltan.Count);
        }
    }
}
