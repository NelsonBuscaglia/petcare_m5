namespace PetCare.Features.Deberes.Domain;

public sealed record DeberActivoVista(int Id, TipoDeber Tipo, TimeOnly Hora, Repeticion Repeticion);

public static class DeberActivo
{
    /// <summary>
    /// Deber activo del PRD: "todos los días" siempre; "solo hoy" mientras su instancia de hoy siga
    /// vigente (existe y no tiene registro de cumplimiento).
    /// </summary>
    public static bool EsActivo(Repeticion repeticion, bool tieneInstanciaVigenteHoy) =>
        repeticion == Repeticion.TodosLosDias
        || (repeticion == Repeticion.SoloHoy && tieneInstanciaVigenteHoy);

    /// <summary>Texto de la lista de configuración, p. ej. "Paseo · 18:00 · todos los días".</summary>
    public static string Texto(DeberActivoVista deber) =>
        $"{TextoTipo(deber.Tipo)} · {deber.Hora:HH\\:mm} · {TextoRepeticion(deber.Repeticion)}";

    private static string TextoTipo(TipoDeber tipo) => tipo switch
    {
        TipoDeber.Comida => "Comida",
        TipoDeber.Paseo => "Paseo",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de deber desconocido."),
    };

    private static string TextoRepeticion(Repeticion repeticion) => repeticion switch
    {
        Repeticion.SoloHoy => "solo para hoy",
        Repeticion.TodosLosDias => "todos los días",
        _ => throw new ArgumentOutOfRangeException(nameof(repeticion), repeticion, "Repetición desconocida."),
    };
}
