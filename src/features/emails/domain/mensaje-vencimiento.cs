using System.Globalization;
using PetCare.Features.Deberes.Domain;

namespace PetCare.Features.Emails.Domain;

public static class MensajeVencimiento
{
    /// <summary>
    /// Email de vencimiento de una instancia (FR-15) para todos los cuidadores del hogar (FR-13).
    /// El asunto no lleva <c>\r</c> ni <c>\n</c> (M5): el nombre de la mascota lo carga el usuario.
    /// </summary>
    public static MensajeEmail Armar(
        string nombreMascota,
        TipoDeber tipo,
        DateOnly fecha,
        TimeOnly hora,
        IReadOnlyList<string> destinatarios)
    {
        var tipoTexto = tipo switch
        {
            TipoDeber.Comida => "comida",
            TipoDeber.Paseo => "paseo",
            _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de deber desconocido."),
        };
        var fechaTexto = fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var horaTexto = hora.ToString("HH:mm", CultureInfo.InvariantCulture);

        var asunto = $"Vencida: {tipoTexto} de {nombreMascota} — {fechaTexto} {horaTexto}"
            .Replace('\r', ' ')
            .Replace('\n', ' ');
        var cuerpo =
            $"La {tipoTexto} de {nombreMascota} del {fechaTexto} a las {horaTexto} venció sin cumplimiento registrado.\n"
            + $"Mascota: {nombreMascota}\n"
            + $"Deber: {tipoTexto}\n"
            + $"Fecha y hora: {fechaTexto} {horaTexto}\n";

        return new MensajeEmail([.. destinatarios], asunto, cuerpo);
    }
}
