using PetCare.Features.Deberes.Domain;

namespace PetCare.Features.EstadoDelDia.Domain;

/// <summary>Estado del día (FR-12): una sección por mascota del hogar.</summary>
public sealed record EstadoDelDia(IReadOnlyList<SeccionMascota> Secciones);

public sealed record SeccionMascota(
    int MascotaId,
    string Nombre,
    IReadOnlyList<InstanciaVista> Instancias,
    int PaseosHoy,
    UltimoRegistro? UltimaComida,
    UltimoRegistro? UltimoPaseo,
    IReadOnlyList<RegistroVista> RegistrosHoy)
{
    public bool SinTareasPendientes => Instancias.Count == 0;
}

/// <summary>Instancia vigente de hoy; <see cref="Pendiente"/> si ya venció (FR-10, FR-13).</summary>
public sealed record InstanciaVista(int Id, TipoDeber Tipo, TimeOnly Hora, bool Pendiente);

/// <summary>Último cumplimiento registrado de un tipo, de cualquier día (FR-15, FR-16).</summary>
public sealed record UltimoRegistro(DateOnly Fecha, TimeOnly HoraReal);

/// <summary>Registro de cumplimiento de hoy, eliminable desde el estado del día (FR-08, FR-09).</summary>
public sealed record RegistroVista(int Id, TipoDeber Tipo, TimeOnly HoraReal);
