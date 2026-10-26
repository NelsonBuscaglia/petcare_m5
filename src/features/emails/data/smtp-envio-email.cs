using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PetCare.Features.Emails.Domain;
using PetCare.Shared.Resultados;

namespace PetCare.Features.Emails.Data;

/// <summary>
/// Envío real por SMTP con MailKit (solo fuera de Development). TLS obligatorio: <c>StartTls</c>, o
/// <c>SslOnConnect</c> en el puerto 465, con la validación de certificados por defecto de MailKit
/// (M6). El remitente va en To y los destinatarios en Bcc (M7).
/// </summary>
public sealed class SmtpEnvioEmail : IEnvioEmail
{
    private const int TimeoutMs = 30_000;
    private const int PuertoSmtps = 465;

    private readonly string host;
    private readonly int puerto;
    private readonly string remitente;
    private readonly (string Usuario, string Clave)? credenciales;
    private readonly SecureSocketOptions seguridad;
    private readonly int timeoutMs;

    public SmtpEnvioEmail(OpcionesSmtp opciones)
        : this(
            opciones,
            opciones?.Puerto == PuertoSmtps ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
            TimeoutMs)
    {
    }

    /// <summary>Solo para tests: permite <c>SecureSocketOptions.None</c> contra un servidor local y un timeout corto.</summary>
    internal SmtpEnvioEmail(OpcionesSmtp opciones, SecureSocketOptions seguridad, int timeoutMs)
    {
        // ValidacionOpcionesEmail ya lo garantiza al arrancar: si falta, es un error de programación.
        ArgumentNullException.ThrowIfNull(opciones);
        ArgumentException.ThrowIfNullOrWhiteSpace(opciones.Host);
        ArgumentException.ThrowIfNullOrWhiteSpace(opciones.Remitente);
        host = opciones.Host;
        puerto = opciones.Puerto;
        remitente = opciones.Remitente;
        if (!string.IsNullOrEmpty(opciones.Usuario))
        {
            ArgumentException.ThrowIfNullOrEmpty(opciones.Clave);
            credenciales = (opciones.Usuario, opciones.Clave);
        }

        this.seguridad = seguridad;
        this.timeoutMs = timeoutMs;
    }

    public async Task<Resultado<bool, EnvioEmailError>> EnviarAsync(MensajeEmail mensaje, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mensaje);
        using var mime = Armar(mensaje);
        try
        {
            using var cliente = new SmtpClient { Timeout = timeoutMs };
            await cliente.ConnectAsync(host, puerto, seguridad, ct);
            if (credenciales is { } c)
            {
                await cliente.AuthenticateAsync(c.Usuario, c.Clave, ct);
            }

            await cliente.SendAsync(mime, ct);
            await DesconectarAsync(cliente, ct);
            return Resultado<bool, EnvioEmailError>.Ok(true);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested && Clasificar(ex) is { } error)
        {
            // Falla esperable del servicio: vuelve tipada y sin el mensaje de la excepción (puede
            // traer direcciones). La cancelación del token del llamador no se traduce: la resuelve él.
            return Resultado<bool, EnvioEmailError>.Falla([error]);
        }
    }

    /// <summary>
    /// El servidor ya aceptó el email (250 al DATA): una falla del servicio al desconectar —incluido
    /// el timeout o la cancelación esperando la respuesta del QUIT— no lo vuelve fallido; si no, la
    /// próxima pasada lo reenviaría. Las excepciones no clasificadas se siguen propagando.
    /// </summary>
    private static async Task DesconectarAsync(SmtpClient cliente, CancellationToken ct)
    {
        try
        {
            await cliente.DisconnectAsync(quit: true, ct);
        }
        catch (Exception ex) when (Clasificar(ex) is not null)
        {
            // Sin log ni error: el envío ya cuenta como aceptado (FR-17) y el SmtpClient se descarta igual.
        }
    }

    /// <summary>
    /// Traduce las fallas del servicio SMTP a un error tipado; <c>null</c> para las que no son del
    /// servicio (errores de programación), que se propagan.
    /// </summary>
    internal static EnvioEmailError? Clasificar(Exception ex) => ex switch
    {
        SmtpCommandException or SmtpProtocolException or AuthenticationException or SslHandshakeException
            or IOException or SocketException => EnvioEmailError.Rechazado,
        OperationCanceledException or TimeoutException => EnvioEmailError.SinRespuesta,
        _ => null,
    };

    private MimeMessage Armar(MensajeEmail mensaje)
    {
        var desde = MailboxAddress.Parse(remitente);
        var mime = new MimeMessage { Subject = mensaje.Asunto, Body = new TextPart("plain") { Text = mensaje.Cuerpo } };
        mime.From.Add(desde);
        mime.To.Add(desde.Clone());
        foreach (var destinatario in mensaje.Para)
        {
            mime.Bcc.Add(MailboxAddress.Parse(destinatario));
        }

        return mime;
    }
}
