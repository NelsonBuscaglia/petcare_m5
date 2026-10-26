using PetCare.Features.Emails.Data;
using PetCare.Features.Emails.Domain;

namespace PetCare.Tests.Features.Emails;

public class ConsolaEnvioEmailTests
{
    [Fact]
    public async Task Consola_EscribeDestinatariosAsuntoYCuerpo()
    {
        using var salida = new StringWriter();
        var envio = new ConsolaEnvioEmail(salida);
        var mensaje = new MensajeEmail(
            ["ana@ejemplo.com", "beto@ejemplo.com"],
            "Vencida: comida de Firulais — 25/10/2026 09:00",
            "Cuerpo del email de prueba.");

        var resultado = await envio.EnviarAsync(mensaje, CancellationToken.None);

        Assert.True(resultado.EsOk);
        var texto = salida.ToString();
        Assert.Contains("ana@ejemplo.com", texto);
        Assert.Contains("beto@ejemplo.com", texto);
        Assert.Contains("Vencida: comida de Firulais — 25/10/2026 09:00", texto);
        Assert.Contains("Cuerpo del email de prueba.", texto);
    }
}
