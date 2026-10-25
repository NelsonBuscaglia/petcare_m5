using System.Globalization;
using PetCare.Features.Deberes.Domain;

namespace PetCare.Features.EstadoDelDia.Domain;

/// <summary>Textos de la pantalla del estado del día.</summary>
public static class TextosEstadoDelDia
{
    /// <summary>Paseos registrados hoy (FR-14): "0 paseos", "1 paseo", "2 paseos".</summary>
    public static string Paseos(int cantidad) =>
        cantidad == 1 ? "1 paseo" : $"{cantidad.ToString(CultureInfo.InvariantCulture)} paseos";

    /// <summary>"dd/MM/yyyy HH:mm", independiente de la cultura del servidor.</summary>
    public static string FechaHora(UltimoRegistro registro) =>
        registro.Fecha.ToDateTime(registro.HoraReal).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string Tipo(TipoDeber tipo) => tipo switch
    {
        TipoDeber.Comida => "Comida",
        TipoDeber.Paseo => "Paseo",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de deber desconocido."),
    };

    /// <summary>Instancia de la lista, p. ej. "Comida · 08:00".</summary>
    public static string Instancia(InstanciaVista instancia) =>
        $"{Tipo(instancia.Tipo)} · {instancia.Hora.ToString("HH:mm", CultureInfo.InvariantCulture)}";

    public static string UltimaComida(UltimoRegistro? registro) =>
        registro is null ? "Sin comidas registradas" : $"Última comida: {FechaHora(registro)}";

    public static string UltimoPaseo(UltimoRegistro? registro) =>
        registro is null ? "Sin paseos registrados" : $"Último paseo: {FechaHora(registro)}";
}
