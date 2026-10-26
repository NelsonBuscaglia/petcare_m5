using PetCare.Shared.Resultados;

namespace PetCare.Features.Emails.Domain;

/// <summary>Email listo para entregar al servicio de envío.</summary>
public sealed record MensajeEmail(IReadOnlyList<string> Para, string Asunto, string Cuerpo);

/// <summary>Por qué el servicio de envío no aceptó un email (FR-16, FR-18).</summary>
public enum EnvioEmailError
{
    Rechazado,
    SinRespuesta,
}

/// <summary>
/// Servicio de envío de emails. <c>Ok</c> significa que el servicio aceptó el email (FR-17); los
/// errores esperables del servicio vuelven tipados y cualquier otra excepción se propaga.
/// </summary>
public interface IEnvioEmail
{
    Task<Resultado<bool, EnvioEmailError>> EnviarAsync(MensajeEmail mensaje, CancellationToken ct);
}
