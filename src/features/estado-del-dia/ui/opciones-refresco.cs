namespace PetCare.Features.EstadoDelDia.Ui;

/// <summary>
/// Sección <c>EstadoDelDia</c> de la configuración: cada cuántos segundos se refresca solo el estado
/// del día (FR-01).
/// </summary>
public sealed class OpcionesRefresco
{
    public const string Seccion = "EstadoDelDia";

    public const int MinimoSegundos = 1;

    /// <summary>NFR-02: un cambio debe verse en ≤ 15 s, así que el intervalo no puede superarlos.</summary>
    public const int MaximoSegundos = 15;

    public int RefrescoSegundos { get; set; } = 10;
}

public static class ValidacionOpcionesRefresco
{
    /// <summary>Errores de la configuración del refresco (vacío si es válida).</summary>
    public static IReadOnlyList<string> Validar(OpcionesRefresco opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        return opciones.RefrescoSegundos is < OpcionesRefresco.MinimoSegundos or > OpcionesRefresco.MaximoSegundos
            ? [$"{OpcionesRefresco.Seccion}:RefrescoSegundos debe estar entre {OpcionesRefresco.MinimoSegundos} y {OpcionesRefresco.MaximoSegundos}."]
            : [];
    }
}
