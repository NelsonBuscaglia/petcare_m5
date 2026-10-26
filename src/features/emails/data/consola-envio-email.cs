using System.Text;
using PetCare.Features.Emails.Domain;
using PetCare.Shared.Resultados;

namespace PetCare.Features.Emails.Data;

/// <summary>
/// Envío de desarrollo (AGENTS.md: los emails se escriben en la consola). Escribe en un
/// <see cref="TextWriter"/> y no en <c>ILogger</c>: es el único lugar donde aparecen los
/// destinatarios (M3 reformulada). Siempre acepta.
/// </summary>
public sealed class ConsolaEnvioEmail(TextWriter salida) : IEnvioEmail
{
    public ConsolaEnvioEmail()
        : this(Console.Out)
    {
    }

    public async Task<Resultado<bool, EnvioEmailError>> EnviarAsync(MensajeEmail mensaje, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mensaje);
        // Un solo Write por email: con envíos concurrentes no se intercalan líneas de dos emails.
        var texto = new StringBuilder()
            .AppendLine("----- Email (modo Consola, no se envió) -----")
            .Append("Para: ").AppendLine(string.Join(", ", mensaje.Para))
            .Append("Asunto: ").AppendLine(mensaje.Asunto)
            .AppendLine()
            .AppendLine(mensaje.Cuerpo.TrimEnd())
            .AppendLine("----- Fin del email -----")
            .ToString();
        await salida.WriteAsync(texto.AsMemory(), ct);
        await salida.FlushAsync(ct);
        return Resultado<bool, EnvioEmailError>.Ok(true);
    }
}
