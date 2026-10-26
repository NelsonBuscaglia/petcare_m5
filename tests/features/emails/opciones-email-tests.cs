using PetCare.Features.Emails.Domain;

namespace PetCare.Tests.Features.Emails;

public class OpcionesEmailTests
{
    [Fact]
    public void ValidarOpciones_ConsolaPorDefecto_Ok()
    {
        var porDefecto = new OpcionesEmail();

        Assert.Equal("Consola", porDefecto.Modo);
        Assert.Empty(ValidacionOpcionesEmail.Validar(porDefecto, esDevelopment: true));
        Assert.Empty(ValidacionOpcionesEmail.Validar(porDefecto, esDevelopment: false));
    }

    [Theory]
    [InlineData("smtp")]
    [InlineData("consola")]
    [InlineData("Otro")]
    [InlineData("")]
    public void ValidarOpciones_ModoDesconocido_Error(string modo)
    {
        var errores = ValidacionOpcionesEmail.Validar(new OpcionesEmail { Modo = modo }, esDevelopment: false);

        var error = Assert.Single(errores);
        Assert.Contains("Email:Modo", error);
    }

    [Fact]
    public void ValidarOpciones_DevelopmentConSmtp_Error()
    {
        var errores = ValidacionOpcionesEmail.Validar(SmtpCompleto(), esDevelopment: true);

        var error = Assert.Single(errores);
        Assert.Contains("Development", error);
    }

    [Fact]
    public void ValidarOpciones_SmtpSinHostORemitente_Error()
    {
        var sinHost = SmtpCompleto();
        sinHost.Smtp.Host = " ";
        Assert.Contains("Email:Smtp:Host", Assert.Single(ValidacionOpcionesEmail.Validar(sinHost, esDevelopment: false)));

        foreach (var remitente in new[] { null, "", "sin-arroba", "@ejemplo.com", "avisos@", "a b@ejemplo.com" })
        {
            var opciones = SmtpCompleto();
            opciones.Smtp.Remitente = remitente;
            Assert.Contains("Email:Smtp:Remitente", Assert.Single(ValidacionOpcionesEmail.Validar(opciones, esDevelopment: false)));
        }

        var ambos = SmtpCompleto();
        ambos.Smtp.Host = null;
        ambos.Smtp.Remitente = null;
        Assert.Equal(2, ValidacionOpcionesEmail.Validar(ambos, esDevelopment: false).Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    [InlineData(-1)]
    public void ValidarOpciones_PuertoInvalido_Error(int puerto)
    {
        var opciones = SmtpCompleto();
        opciones.Smtp.Puerto = puerto;

        Assert.Contains("Email:Smtp:Puerto", Assert.Single(ValidacionOpcionesEmail.Validar(opciones, esDevelopment: false)));
    }

    [Fact]
    public void ValidarOpciones_UsuarioSinClave_Error()
    {
        var opciones = SmtpCompleto();
        opciones.Smtp.Clave = "";

        var error = Assert.Single(ValidacionOpcionesEmail.Validar(opciones, esDevelopment: false));
        Assert.Contains("Email:Smtp:Clave", error);
        Assert.DoesNotContain("avisos-usuario", error);
    }

    [Fact]
    public void ValidarOpciones_SmtpCompletoFueraDeDevelopment_Ok()
    {
        Assert.Empty(ValidacionOpcionesEmail.Validar(SmtpCompleto(), esDevelopment: false));

        var sinUsuario = SmtpCompleto();
        sinUsuario.Smtp.Usuario = null;
        sinUsuario.Smtp.Clave = null;
        sinUsuario.Smtp.Puerto = 465;
        Assert.Empty(ValidacionOpcionesEmail.Validar(sinUsuario, esDevelopment: false));
        Assert.Equal(587, new OpcionesSmtp().Puerto);
    }

    private static OpcionesEmail SmtpCompleto() => new()
    {
        Modo = "Smtp",
        Smtp = new OpcionesSmtp
        {
            Host = "smtp.ejemplo.com",
            Puerto = 587,
            Usuario = "avisos-usuario",
            Clave = "clave-secreta",
            Remitente = "avisos@ejemplo.com",
        },
    };
}
