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
/// Servidor SMTP mínimo sobre <see cref="TcpListener"/> en loopback (puerto libre), sin TLS:
/// alcanza para que MailKit hable con él usando <c>SecureSocketOptions.None</c>. Registra los
/// <c>RCPT TO</c> y el contenido de <c>DATA</c>. Con credenciales, anuncia <c>AUTH PLAIN LOGIN</c>
/// y exige autenticarse antes del <c>MAIL FROM</c> (530 si no).
/// </summary>
public sealed class ServidorSmtpFalso : IAsyncDisposable
{
    private readonly TcpListener escucha = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource fin = new();
    private readonly ModoServidorSmtp modo;
    private readonly ConcurrentQueue<string> destinatarios = new();
    private readonly ConcurrentQueue<string> datos = new();
    private readonly ConcurrentQueue<IntentoDeAutenticacion> autenticaciones = new();
    private readonly (string Usuario, string Clave)? credenciales;
    private readonly Task atencion;

    public ServidorSmtpFalso(ModoServidorSmtp modo, (string Usuario, string Clave)? credenciales = null)
    {
        this.modo = modo;
        this.credenciales = credenciales;
        escucha.Start();
        atencion = AtenderAsync(fin.Token);
    }

    public string Host => "127.0.0.1";

    public int Puerto => ((IPEndPoint)escucha.LocalEndpoint).Port;

    /// <summary>Direcciones recibidas en cada <c>RCPT TO</c>.</summary>
    public IReadOnlyList<string> Destinatarios => destinatarios.ToArray();

    /// <summary>Contenido (cabeceras y cuerpo) de cada mensaje recibido por <c>DATA</c>.</summary>
    public IReadOnlyList<string> Mensajes => datos.ToArray();

    /// <summary>Cada <c>AUTH</c> recibido, con las credenciales decodificadas y si se aceptó.</summary>
    public IReadOnlyList<IntentoDeAutenticacion> Autenticaciones => autenticaciones.ToArray();

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
        var autenticado = false;
        while (await lector.ReadLineAsync(ct) is { } linea)
        {
            var comando = linea.ToUpperInvariant();
            if (comando.StartsWith("EHLO", StringComparison.Ordinal) || comando.StartsWith("HELO", StringComparison.Ordinal))
            {
                if (credenciales is not null)
                {
                    await escritor.WriteLineAsync("250-localhost");
                    await escritor.WriteLineAsync("250 AUTH PLAIN LOGIN");
                }
                else
                {
                    await escritor.WriteLineAsync("250 localhost");
                }
            }
            else if (comando.StartsWith("AUTH ", StringComparison.Ordinal) && credenciales is { } esperadas)
            {
                var recibidas = await LeerCredencialesAsync(linea, lector, escritor, ct);
                autenticado = recibidas == esperadas;
                autenticaciones.Enqueue(new IntentoDeAutenticacion(recibidas.Usuario, recibidas.Clave, autenticado));
                await escritor.WriteLineAsync(autenticado
                    ? "235 2.7.0 Autenticado"
                    : "535 5.7.8 Credenciales invalidas");
            }
            else if (comando.StartsWith("MAIL FROM:", StringComparison.Ordinal))
            {
                if (credenciales is not null && !autenticado)
                {
                    await escritor.WriteLineAsync("530 5.7.0 Autenticacion requerida");
                    continue;
                }

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

    /// <summary>Lee las credenciales de un <c>AUTH PLAIN</c> (con o sin respuesta inicial) o <c>AUTH LOGIN</c>.</summary>
    private static async Task<(string Usuario, string Clave)> LeerCredencialesAsync(
        string linea, StreamReader lector, StreamWriter escritor, CancellationToken ct)
    {
        var partes = linea.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var mecanismo = partes[1].ToUpperInvariant();
        if (mecanismo == "PLAIN")
        {
            var inicial = partes.Length > 2 ? partes[2] : await PedirAsync("", lector, escritor, ct);
            var campos = Decodificar(inicial).Split('\0');
            return (campos[1], campos[2]);
        }

        var usuario = partes.Length > 2 ? partes[2] : await PedirAsync("VXNlcm5hbWU6", lector, escritor, ct);
        var clave = await PedirAsync("UGFzc3dvcmQ6", lector, escritor, ct);
        return (Decodificar(usuario), Decodificar(clave));
    }

    private static async Task<string> PedirAsync(string desafio, StreamReader lector, StreamWriter escritor, CancellationToken ct)
    {
        await escritor.WriteLineAsync($"334 {desafio}");
        return await lector.ReadLineAsync(ct) ?? throw new IOException("El cliente cortó durante el AUTH.");
    }

    private static string Decodificar(string base64) => Encoding.UTF8.GetString(Convert.FromBase64String(base64.Trim()));
}

/// <summary>Un <c>AUTH</c> recibido por <see cref="ServidorSmtpFalso"/>.</summary>
public sealed record IntentoDeAutenticacion(string Usuario, string Clave, bool Aceptado);
