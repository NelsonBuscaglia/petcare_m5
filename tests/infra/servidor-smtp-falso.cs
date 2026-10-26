using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PetCare.Tests.Infra;

public enum ModoServidorSmtp
{
    /// <summary>Responde 250 a todo y acepta el mensaje.</summary>
    Acepta,

    /// <summary>Responde 550 al <c>MAIL FROM</c>.</summary>
    Rechaza550,

    /// <summary>Acepta la conexión TCP y nunca escribe nada (ni el saludo).</summary>
    NoResponde,

    /// <summary>Como <see cref="Acepta"/>, pero tras el 250 del <c>DATA</c> no contesta nada más (ni el <c>QUIT</c>).</summary>
    CallaTrasData,
}

/// <summary>
/// Servidor SMTP mínimo sobre <see cref="TcpListener"/> en loopback (puerto libre), sin TLS ni
/// autenticación: alcanza para que MailKit hable con él usando <c>SecureSocketOptions.None</c>.
/// Registra los <c>RCPT TO</c> y el contenido de <c>DATA</c>.
/// </summary>
public sealed class ServidorSmtpFalso : IAsyncDisposable
{
    private readonly TcpListener escucha = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource fin = new();
    private readonly ModoServidorSmtp modo;
    private readonly ConcurrentQueue<string> destinatarios = new();
    private readonly ConcurrentQueue<string> datos = new();
    private readonly Task atencion;

    public ServidorSmtpFalso(ModoServidorSmtp modo)
    {
        this.modo = modo;
        escucha.Start();
        atencion = AtenderAsync(fin.Token);
    }

    public string Host => "127.0.0.1";

    public int Puerto => ((IPEndPoint)escucha.LocalEndpoint).Port;

    /// <summary>Direcciones recibidas en cada <c>RCPT TO</c>.</summary>
    public IReadOnlyList<string> Destinatarios => destinatarios.ToArray();

    /// <summary>Contenido (cabeceras y cuerpo) de cada mensaje recibido por <c>DATA</c>.</summary>
    public IReadOnlyList<string> Mensajes => datos.ToArray();

    public async ValueTask DisposeAsync()
    {
        await fin.CancelAsync();
        escucha.Stop();
        try
        {
            await atencion;
        }
        catch (OperationCanceledException)
        {
            // Fin pedido por el Dispose: no es un error del test.
        }

        fin.Dispose();
    }

    private async Task AtenderAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient cliente;
            try
            {
                cliente = await escucha.AcceptTcpClientAsync(ct);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException
                                       && ct.IsCancellationRequested)
            {
                return;
            }

            using (cliente)
            {
                try
                {
                    await ConversarAsync(cliente, ct);
                }
                catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException)
                {
                    // El cliente cortó (timeout de MailKit) o el servidor se está cerrando.
                }
            }
        }
    }

    private async Task ConversarAsync(TcpClient cliente, CancellationToken ct)
    {
        var stream = cliente.GetStream();
        if (modo == ModoServidorSmtp.NoResponde)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return;
        }

        using var lector = new StreamReader(stream, Encoding.ASCII);
        await using var escritor = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
        await escritor.WriteLineAsync("220 localhost ESMTP falso");
        while (await lector.ReadLineAsync(ct) is { } linea)
        {
            var comando = linea.ToUpperInvariant();
            if (comando.StartsWith("EHLO", StringComparison.Ordinal) || comando.StartsWith("HELO", StringComparison.Ordinal))
            {
                await escritor.WriteLineAsync("250 localhost");
            }
            else if (comando.StartsWith("MAIL FROM:", StringComparison.Ordinal))
            {
                await escritor.WriteLineAsync(modo == ModoServidorSmtp.Rechaza550
                    ? "550 5.7.1 Rechazado por el servidor falso"
                    : "250 OK");
            }
            else if (comando.StartsWith("RCPT TO:", StringComparison.Ordinal))
            {
                destinatarios.Enqueue(linea["RCPT TO:".Length..].Trim().Trim('<', '>'));
                await escritor.WriteLineAsync("250 OK");
            }
            else if (comando == "DATA")
            {
                await escritor.WriteLineAsync("354 Fin con <CRLF>.<CRLF>");
                var contenido = new StringBuilder();
                while (await lector.ReadLineAsync(ct) is { } dato && dato != ".")
                {
                    contenido.Append(dato).Append("\r\n");
                }

                datos.Enqueue(contenido.ToString());
                await escritor.WriteLineAsync("250 OK aceptado");
                if (modo == ModoServidorSmtp.CallaTrasData)
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                    return;
                }
            }
            else if (comando == "QUIT")
            {
                await escritor.WriteLineAsync("221 Chau");
                return;
            }
            else
            {
                await escritor.WriteLineAsync(comando == "RSET" || comando == "NOOP" ? "250 OK" : "502 No implementado");
            }
        }
    }
}
