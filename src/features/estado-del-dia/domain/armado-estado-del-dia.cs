using PetCare.Features.Deberes.Domain;
using PetCare.Features.Hogar.Domain;
using PetCare.Features.Instancias.Domain;

namespace PetCare.Features.EstadoDelDia.Domain;

/// <summary>Instancia de hoy tal como la lee el servicio, con la mascota y el tipo de su deber.</summary>
public sealed record InstanciaLeida(int Id, int MascotaId, TipoDeber Tipo, DateOnly Fecha, TimeOnly Hora, bool TieneRegistro);

/// <summary>Registro de cumplimiento tal como lo lee el servicio, con la mascota y el tipo de su deber.</summary>
public sealed record RegistroLeido(int MascotaId, TipoDeber Tipo, DateOnly Fecha, TimeOnly HoraReal);

public static class ArmadoEstadoDelDia
{
    /// <summary>
    /// Una sección por mascota, en el orden recibido, aunque no tenga instancias (FR-12, FR-17):
    /// sus instancias vigentes por hora y tipo, marcadas si vencieron (FR-13, FR-10), los paseos
    /// registrados hoy (FR-14) y el último registro de comida y de paseo de cualquier día (FR-15, FR-16).
    /// </summary>
    public static EstadoDelDia Armar(
        IReadOnlyList<MascotaVista> mascotas,
        IReadOnlyList<InstanciaLeida> instancias,
        IReadOnlyList<RegistroLeido> registros,
        DateTime ahoraLocal)
    {
        var hoy = DateOnly.FromDateTime(ahoraLocal);
        var secciones = mascotas
            .Select(m => ArmarSeccion(
                m,
                instancias.Where(i => i.MascotaId == m.Id),
                registros.Where(r => r.MascotaId == m.Id).ToList(),
                hoy,
                ahoraLocal))
            .ToList();
        return new EstadoDelDia(secciones);
    }

    private static SeccionMascota ArmarSeccion(
        MascotaVista mascota,
        IEnumerable<InstanciaLeida> instancias,
        IReadOnlyList<RegistroLeido> registros,
        DateOnly hoy,
        DateTime ahoraLocal)
    {
        var vigentes = instancias
            .Where(i => EstadoInstancia.EsVigente(i.Fecha, i.TieneRegistro, hoy))
            .OrderBy(i => i.Hora)
            .ThenBy(i => i.Tipo)
            .Select(i => new InstanciaVista(i.Id, i.Tipo, i.Hora, EstadoInstancia.EsVencida(i.Fecha, i.Hora, ahoraLocal)))
            .ToList();
        var paseosHoy = registros.Count(r => r.Tipo == TipoDeber.Paseo && r.Fecha == hoy);

        return new SeccionMascota(
            mascota.Id,
            mascota.Nombre,
            vigentes,
            paseosHoy,
            Ultimo(registros, TipoDeber.Comida),
            Ultimo(registros, TipoDeber.Paseo));
    }

    private static UltimoRegistro? Ultimo(IReadOnlyList<RegistroLeido> registros, TipoDeber tipo) =>
        registros
            .Where(r => r.Tipo == tipo)
            .OrderByDescending(r => r.Fecha)
            .ThenByDescending(r => r.HoraReal)
            .Select(r => new UltimoRegistro(r.Fecha, r.HoraReal))
            .FirstOrDefault();
}
