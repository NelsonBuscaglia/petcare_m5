namespace PetCare.Features.EstadoDelDia.Domain;

/// <summary>
/// Modelo del parcial <c>_estado-del-dia</c>: el estado, un mensaje de rechazo (si hubo) y el
/// formulario de cumplimiento abierto (si hay uno).
/// </summary>
public sealed record EstadoDelDiaVista(
    EstadoDelDia Estado,
    string? Mensaje,
    FormularioCumplimientoVista? Formulario,
    int RefrescoSegundos);

/// <summary>
/// Formulario de cumplimiento abierto en una instancia: la hora a mostrar (la actual al abrirlo,
/// o la ingresada si fue rechazada) y el error de la hora, si lo hubo.
/// </summary>
public sealed record FormularioCumplimientoVista(int InstanciaId, string HoraPropuesta, string? Error);
