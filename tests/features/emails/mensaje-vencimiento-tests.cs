using PetCare.Features.Deberes.Domain;
using PetCare.Features.Emails.Domain;

namespace PetCare.Tests.Features.Emails;

public class MensajeVencimientoTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly TimeOnly Nueve = new(9, 0);
    private static readonly string[] AnaYBeto = ["ana@ejemplo.com", "beto@ejemplo.com"];

    [Fact]
    public void Armar_ComidaFirulais0900_IncluyeDatos()
    {
        var mensaje = MensajeVencimiento.Armar("Firulais", TipoDeber.Comida, Hoy, Nueve, AnaYBeto);

        Assert.Equal("Vencida: comida de Firulais — 25/10/2026 09:00", mensaje.Asunto);
        Assert.Contains("Firulais", mensaje.Cuerpo);
        Assert.Contains("comida", mensaje.Cuerpo);
        Assert.Contains("25/10/2026", mensaje.Cuerpo);
        Assert.Contains("09:00", mensaje.Cuerpo);

        var paseo = MensajeVencimiento.Armar("Michi", TipoDeber.Paseo, Hoy, new TimeOnly(18, 30), AnaYBeto);
        Assert.Equal("Vencida: paseo de Michi — 25/10/2026 18:30", paseo.Asunto);
        Assert.Contains("paseo", paseo.Cuerpo);
    }

    [Fact]
    public void Armar_DestinatariosTodosLosCuidadores()
    {
        var mensaje = MensajeVencimiento.Armar("Firulais", TipoDeber.Comida, Hoy, Nueve, AnaYBeto);

        Assert.Equal(AnaYBeto, mensaje.Para);
    }

    [Fact]
    public void Armar_NombreConSaltoDeLinea_AsuntoSinCrLf()
    {
        var mensaje = MensajeVencimiento.Armar("Fi\r\nBcc: x@y", TipoDeber.Comida, Hoy, Nueve, AnaYBeto);

        Assert.DoesNotContain('\r', mensaje.Asunto);
        Assert.DoesNotContain('\n', mensaje.Asunto);
        Assert.Equal("Vencida: comida de Fi  Bcc: x@y — 25/10/2026 09:00", mensaje.Asunto);
    }
}
