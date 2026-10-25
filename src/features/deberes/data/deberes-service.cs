using Microsoft.EntityFrameworkCore;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Instancias.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Hosting;
using PetCare.Shared.Resultados;
using PetCare.Shared.Tiempo;

namespace PetCare.Features.Deberes.Data;

public sealed class DeberesService(PetCareDbContext db, TimeProvider time, GeneracionLock generacionLock)
{
    /// <summary>
    /// Da de alta el deber y crea su instancia de hoy (FR-07) en un único guardado. Los errores de
    /// base no se capturan: llegan al manejador global.
    /// </summary>
    public async Task<Resultado<int, AltaDeberError>> DarDeAltaAsync(
        int mascotaId,
        string? tipo,
        string? hora,
        string? repeticion,
        CancellationToken cancellationToken)
    {
        if (!await db.Mascotas.AnyAsync(m => m.Id == mascotaId, cancellationToken))
        {
            return Resultado<int, AltaDeberError>.Falla([AltaDeberError.MascotaNoEncontrada]);
        }

        var validacion = ValidacionAltaDeber.Validar(tipo, hora, repeticion);
        if (!validacion.EsOk)
        {
            return Resultado<int, AltaDeberError>.Falla(validacion.Errores);
        }

        var alta = validacion.Valor;
        using (await generacionLock.TomarAsync(cancellationToken))
        {
            var hoy = ZonaHogar.Hoy(time.GetUtcNow());
            var deber = new Deber
            {
                MascotaId = mascotaId,
                Tipo = alta.Tipo,
                Hora = alta.Hora,
                Repeticion = alta.Repeticion,
                FechaAlta = hoy,
            };
            db.Deberes.Add(deber);
            var instancia = new Instancia { Fecha = hoy, Hora = alta.Hora };
            db.Instancias.Add(instancia);

            // Sin propiedades de navegación: la instancia apunta a la clave temporal del deber y EF
            // la reemplaza por la real al insertar, dentro del mismo SaveChanges (una transacción).
            var claveDeber = db.Entry(deber).Property(d => d.Id);
            var fkInstancia = db.Entry(instancia).Property(i => i.DeberId);
            fkInstancia.CurrentValue = claveDeber.CurrentValue;
            fkInstancia.IsTemporary = true;

            await db.SaveChangesAsync(cancellationToken);
            return Resultado<int, AltaDeberError>.Ok(deber.Id);
        }
    }

    /// <summary>Deberes activos de la mascota (FR-03, FR-06), ordenados por hora y luego tipo.</summary>
    public async Task<IReadOnlyList<DeberActivoVista>> ListarActivosAsync(int mascotaId, CancellationToken cancellationToken)
    {
        var hoy = ZonaHogar.Hoy(time.GetUtcNow());
        var filas = await db.Deberes
            .Where(d => d.MascotaId == mascotaId)
            .Select(d => new
            {
                d.Id,
                d.Tipo,
                d.Hora,
                d.Repeticion,
                TieneInstanciaVigenteHoy = db.Instancias.Any(i =>
                    i.DeberId == d.Id
                    && i.Fecha == hoy
                    && !db.RegistrosCumplimiento.Any(r => r.InstanciaId == i.Id)),
            })
            .ToListAsync(cancellationToken);

        return filas
            .Where(f => DeberActivo.EsActivo(f.Repeticion, f.TieneInstanciaVigenteHoy))
            .OrderBy(f => f.Hora)
            .ThenBy(f => f.Tipo)
            .Select(f => new DeberActivoVista(f.Id, f.Tipo, f.Hora, f.Repeticion))
            .ToList();
    }
}
