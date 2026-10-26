using System.Text.RegularExpressions;

namespace PetCare.Features.Emails.Domain;

/// <summary>
/// Sección <c>Email</c> de la configuración. <c>Smtp</c> se configura solo por variables de entorno
/// (<c>Email__Smtp__Host</c>, …), nunca en archivos del repo (M3-bis).
/// </summary>
public sealed class OpcionesEmail
{
    public const string ModoConsola = "Consola";
    public const string ModoSmtp = "Smtp";

    public string Modo { get; set; } = ModoConsola;

    public OpcionesSmtp Smtp { get; set; } = new();
}

public sealed class OpcionesSmtp
{
    public string? Host { get; set; }

    public int Puerto { get; set; } = 587;

    public string? Usuario { get; set; }

    public string? Clave { get; set; }

    public string? Remitente { get; set; }
}

public static partial class ValidacionOpcionesEmail
{
    /// <summary>
    /// Errores de la configuración de email (vacío si es válida). Los mensajes nombran claves, nunca
    /// valores: la configuración puede traer credenciales.
    /// </summary>
    public static IReadOnlyList<string> Validar(OpcionesEmail opciones, bool esDevelopment)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        if (opciones.Modo is not (OpcionesEmail.ModoConsola or OpcionesEmail.ModoSmtp))
        {
            return [$"Email:Modo debe ser exactamente \"{OpcionesEmail.ModoConsola}\" o \"{OpcionesEmail.ModoSmtp}\"."];
        }

        if (opciones.Modo == OpcionesEmail.ModoConsola)
        {
            return [];
        }

        if (esDevelopment)
        {
            return [$"En Development Email:Modo debe ser \"{OpcionesEmail.ModoConsola}\": no se mandan emails reales."];
        }

        var smtp = opciones.Smtp ?? new OpcionesSmtp();
        var errores = new List<string>();
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            errores.Add("Email:Smtp:Host es obligatorio con Email:Modo=Smtp.");
        }

        if (smtp.Remitente is null || !FormatoEmail().IsMatch(smtp.Remitente))
        {
            errores.Add("Email:Smtp:Remitente es obligatorio con Email:Modo=Smtp y debe tener el formato x@y.");
        }

        if (smtp.Puerto is < 1 or > 65535)
        {
            errores.Add("Email:Smtp:Puerto debe estar entre 1 y 65535.");
        }

        if (!string.IsNullOrEmpty(smtp.Usuario) && string.IsNullOrEmpty(smtp.Clave))
        {
            errores.Add("Email:Smtp:Clave es obligatoria cuando hay Email:Smtp:Usuario.");
        }

        return errores;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\z")]
    private static partial Regex FormatoEmail();
}
