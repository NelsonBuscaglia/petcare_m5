using System.Diagnostics;
using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using PetCare.Features.Emails.Data;
using PetCare.Features.Emails.Domain;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.Emails;

public class SmtpEnvioEmailTests
{
    private const string Remitente = "avisos@ejemplo.com";
    private const int TimeoutCorto = 500;

    /// <summary>Holgado para los casos que sí responden: con la suite en paralelo, 500 ms no alcanzan.</summary>
    private const int TimeoutHolgado = 10_000;

    /// <summary>El caso sin respuesta al QUIT espera este timeout entero: corto, pero con margen para el envío.</summary>
    private const int TimeoutQuit = 3_000;

    private static readonly MensajeEmail Mensaje = new(
        ["ana@ejemplo.com", "beto@ejemplo.com"],
        "Vencida: comida de Firulais — 25/10/2026 09:00",
        "La comida de Firulais de las 09:00 venció.");

    [Fact]
    public async Task Smtp_ServidorAcepta_Ok()
    {
        await using var servidor = new ServidorSmtpFalso(ModoServidorSmtp.Acepta);

        var resultado = await CrearEnvio(servidor).EnviarAsync(Mensaje, CancellationToken.None);

        Assert.True(resultado.EsOk, $"Errores: {string.Join(", ", resultado.Errores)}");
        // El remitente también es RCPT porque va en To; los cuidadores, solo por Bcc.
        Assert.Equal([Remitente, "ana@ejemplo.com", "beto@ejemplo.com"], servidor.Destinatarios);
        var datos = Assert.Single(servidor.Mensajes);
        var cabeceras = datos[..datos.IndexOf("\r\n\r\n", StringComparison.Ordinal)];
        var to = Assert.Single(cabeceras.Split("\r\n"), l => l.StartsWith("To:", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(Remitente, to);
        Assert.Single(cabeceras.Split("\r\n"), l => l.StartsWith("From:", StringComparison.OrdinalIgnoreCase) && l.Contains(Remitente));

        // M7: ningún destinatario aparece en las cabeceras que reciben los demás.
        Assert.DoesNotContain("ana@ejemplo.com", datos);
        Assert.DoesNotContain("beto@ejemplo.com", datos);
        Assert.DoesNotContain("Bcc:", cabeceras, StringComparison.OrdinalIgnoreCase);

        // Si el servidor ya aceptó el DATA, una falla al desconectar (QUIT sin respuesta) no es un
        // rechazo: el email salió y no debe reintentarse.
        await using var callaTrasData = new ServidorSmtpFalso(ModoServidorSmtp.CallaTrasData);
        var trasData = await CrearEnvio(callaTrasData, TimeoutQuit).EnviarAsync(Mensaje, CancellationToken.None);
        Assert.Single(callaTrasData.Mensajes);
        Assert.True(trasData.EsOk, $"Aceptado y sin respuesta al QUIT, devolvió: {string.Join(", ", trasData.Errores)}");
    }

    [Fact]
    public async Task Smtp_ServidorRechaza550_Rechazado()
    {
        await using var servidor = new ServidorSmtpFalso(ModoServidorSmtp.Rechaza550);

        var resultado = await CrearEnvio(servidor).EnviarAsync(Mensaje, CancellationToken.None);

        Assert.False(resultado.EsOk);
        Assert.Equal([EnvioEmailError.Rechazado], resultado.Errores);
        Assert.Empty(servidor.Mensajes);
    }

    [Fact]
    public async Task Smtp_SinRespuesta_SinRespuesta()
    {
        await using var servidor = new ServidorSmtpFalso(ModoServidorSmtp.NoResponde);
        var cronometro = Stopwatch.StartNew();

        var resultado = await CrearEnvio(servidor, TimeoutCorto).EnviarAsync(Mensaje, CancellationToken.None);

        cronometro.Stop();
        Assert.False(resultado.EsOk);
        Assert.Equal([EnvioEmailError.SinRespuesta], resultado.Errores);
        Assert.True(cronometro.Elapsed >= TimeSpan.FromMilliseconds(TimeoutCorto - 50), $"Falló antes del timeout: {cronometro.Elapsed}.");
        Assert.True(cronometro.Elapsed < TimeSpan.FromSeconds(10), $"No respetó el timeout corto: {cronometro.Elapsed}.");

        // La cancelación del token del llamador no se traduce a SinRespuesta: se propaga.
        using var cancelado = new CancellationTokenSource();
        await cancelado.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CrearEnvio(servidor, TimeoutCorto).EnviarAsync(Mensaje, cancelado.Token));
    }

    [Fact]
    public void Clasificar_ExcepcionesMailKit()
    {
        Assert.Equal(EnvioEmailError.Rechazado, SmtpEnvioEmail.Clasificar(
            new SmtpCommandException(SmtpErrorCode.SenderNotAccepted, SmtpStatusCode.MailboxUnavailable, "550")));
        Assert.Equal(EnvioEmailError.Rechazado, SmtpEnvioEmail.Clasificar(new SmtpProtocolException("protocolo")));
        Assert.Equal(EnvioEmailError.Rechazado, SmtpEnvioEmail.Clasificar(new AuthenticationException("auth")));
        Assert.Equal(EnvioEmailError.Rechazado, SmtpEnvioEmail.Clasificar(new SslHandshakeException("tls")));
        Assert.Equal(EnvioEmailError.Rechazado, SmtpEnvioEmail.Clasificar(new IOException("io")));
        Assert.Equal(EnvioEmailError.Rechazado, SmtpEnvioEmail.Clasificar(new SocketException((int)SocketError.ConnectionRefused)));
        Assert.Equal(EnvioEmailError.SinRespuesta, SmtpEnvioEmail.Clasificar(new OperationCanceledException()));
        Assert.Equal(EnvioEmailError.SinRespuesta, SmtpEnvioEmail.Clasificar(new TimeoutException()));
        Assert.Null(SmtpEnvioEmail.Clasificar(new InvalidOperationException("otra")));
        Assert.Null(SmtpEnvioEmail.Clasificar(new NotSupportedException("no soportado")));
    }

    private static SmtpEnvioEmail CrearEnvio(ServidorSmtpFalso servidor, int timeoutMs = TimeoutHolgado) => new(
        new OpcionesSmtp { Host = servidor.Host, Puerto = servidor.Puerto, Remitente = Remitente },
        SecureSocketOptions.None,
        timeoutMs);
}
