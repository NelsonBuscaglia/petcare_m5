using System.Net;
using System.Text.Json;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetCare.Shared.Data;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Shared;

public class ArranqueTests
{
    private const string MensajeGenerico = "Ocurrió un error inesperado.";

    [Fact]
    public void Arranque_LaunchSettings_SoloLocalhost()
    {
        var src = Path.Combine(RaizDelRepo(), "src");
        var launchSettings = Path.Combine(src, "Properties", "launchSettings.json");

        Assert.True(File.Exists(launchSettings), $"No existe {launchSettings}");
        using var documento = LeerJson(launchSettings);
        var urls = documento.RootElement.GetProperty("profiles").EnumerateObject()
            .Where(p => p.Value.TryGetProperty("applicationUrl", out _))
            .SelectMany(p => p.Value.GetProperty("applicationUrl").GetString()!
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToList();

        Assert.NotEmpty(urls);
        Assert.All(urls, url => Assert.Equal("localhost", new Uri(url).Host));

        foreach (var appsettings in Directory.GetFiles(src, "appsettings*.json"))
        {
            using var config = LeerJson(appsettings);
            if (!config.RootElement.TryGetProperty("Kestrel", out var kestrel)
                || !kestrel.TryGetProperty("Endpoints", out var endpoints))
            {
                continue;
            }

            foreach (var endpoint in endpoints.EnumerateObject())
            {
                var url = endpoint.Value.GetProperty("Url").GetString() ?? "";
                Assert.DoesNotContain("0.0.0.0", url);
                Assert.DoesNotContain("://*", url);
                Assert.DoesNotContain("://+", url);
            }
        }
    }

    [Fact]
    public void Arranque_SinCadenaDeConexion_Falla()
    {
        using var fabricaBase = new PetCareAppFactory(migrar: false);
        using var factory = fabricaBase
            .WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:PetCare", ""));

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Equal("Falta ConnectionStrings:PetCare en la configuración.", error.Message);
    }

    [Fact]
    public void Arranque_DevelopmentConSmtp_Falla()
    {
        using var fabricaBase = new PetCareAppFactory(migrar: false);
        using var factory = fabricaBase.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("Email:Modo", "Smtp");
            b.UseSetting("Email:Smtp:Host", "smtp.ejemplo.com");
            b.UseSetting("Email:Smtp:Remitente", "avisos@ejemplo.com");
        });

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("Development", error.Message);
        Assert.StartsWith("Configuración de email inválida", error.Message);
    }

    [Fact]
    public void Arranque_SmtpSinHost_Falla()
    {
        using var fabricaBase = new PetCareAppFactory(migrar: false);
        using var factory = fabricaBase.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.UseSetting("Email:Modo", "Smtp");
            b.UseSetting("Email:Smtp:Remitente", "avisos@ejemplo.com");
            b.UseSetting("Email:Smtp:Usuario", "avisos-usuario");
            b.UseSetting("Email:Smtp:Clave", "clave-secreta-de-prueba");
        });

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("Email:Smtp:Host", error.Message);
        Assert.DoesNotContain("clave-secreta-de-prueba", error.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("16")]
    public void Arranque_RefrescoInvalido_Falla(string segundos)
    {
        using var fabricaBase = new PetCareAppFactory(migrar: false);
        using var factory = fabricaBase.WithWebHostBuilder(b => b.UseSetting("EstadoDelDia:RefrescoSegundos", segundos));

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Equal(
            "Configuración del estado del día inválida: EstadoDelDia:RefrescoSegundos debe estar entre 1 y 15.",
            error.Message);
    }

    [Fact]
    public void Arranque_AppSettingsSinCredencialesSmtp()
    {
        var src = Path.Combine(RaizDelRepo(), "src");
        using (var appsettings = LeerJson(Path.Combine(src, "appsettings.json")))
        {
            Assert.True(appsettings.RootElement.TryGetProperty("Email", out var email), "appsettings.json no define la sección Email.");
            Assert.Equal("Consola", email.GetProperty("Modo").GetString());
        }

        var archivos = Directory.GetFiles(src, "appsettings*.json")
            .Append(Path.Combine(src, "Properties", "launchSettings.json"));
        foreach (var archivo in archivos)
        {
            using var documento = LeerJson(archivo);
            var claves = ClavesPlanas(documento.RootElement, "").ToList();
            foreach (var prohibida in new[] { "Usuario", "Clave", "Host" })
            {
                Assert.DoesNotContain(claves, c =>
                    c.Equals($"Email:Smtp:{prohibida}", StringComparison.OrdinalIgnoreCase)
                    || c.EndsWith($":Email__Smtp__{prohibida}", StringComparison.OrdinalIgnoreCase));
            }

            Assert.DoesNotContain(claves, c => c.StartsWith("Email:Smtp", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task Error_ExcepcionEnProduccion_Devuelve500Generico()
    {
        using var fabricaBase = new PetCareAppFactory();
        using var factory = fabricaBase.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.ConfigureServices(s => s.AddSingleton<IStartupFilter>(new RutaDeTest(
                "/_test/lanza",
                _ => throw new InvalidOperationException("detalle-secreto-de-la-excepcion"))));
        });
        using var cliente = factory.CreateClient();

        using var respuesta = await cliente.GetAsync("/_test/lanza");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Contains(MensajeGenerico, TextoVisible(cuerpo));
        Assert.DoesNotContain("detalle-secreto-de-la-excepcion", cuerpo);
        Assert.DoesNotContain("InvalidOperationException", cuerpo);
    }

    [Fact]
    public async Task Error_BaseSinMigrar_Devuelve500Generico()
    {
        using var fabricaBase = new PetCareAppFactory(migrar: false);
        using var factory = fabricaBase.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.ConfigureServices(s => s.AddSingleton<IStartupFilter>(new RutaDeTest(
                "/_test/consulta",
                async contexto =>
                {
                    var db = contexto.RequestServices.GetRequiredService<PetCareDbContext>();
                    var cantidad = await db.Mascotas.CountAsync();
                    await contexto.Response.WriteAsync($"mascotas: {cantidad}");
                })));
        });
        using var cliente = factory.CreateClient();

        using var respuesta = await cliente.GetAsync("/_test/consulta");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Contains(MensajeGenerico, TextoVisible(cuerpo));
        Assert.DoesNotContain("SqliteException", cuerpo);
        Assert.DoesNotContain("no such table", cuerpo);
    }

    /// <summary>Claves "A:B:C" de un JSON, como las vería <c>IConfiguration</c>.</summary>
    private static IEnumerable<string> ClavesPlanas(JsonElement elemento, string prefijo)
    {
        if (elemento.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var propiedad in elemento.EnumerateObject())
        {
            var clave = prefijo.Length == 0 ? propiedad.Name : $"{prefijo}:{propiedad.Name}";
            yield return clave;
            foreach (var hija in ClavesPlanas(propiedad.Value, clave))
            {
                yield return hija;
            }
        }
    }

    private static string TextoVisible(string html) =>
        new HtmlParser().ParseDocument(html).Body?.TextContent ?? "";

    private static JsonDocument LeerJson(string ruta) =>
        JsonDocument.Parse(
            File.ReadAllText(ruta),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

    private static string RaizDelRepo()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "petcare.slnx")))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName
            ?? throw new InvalidOperationException("No se encontró petcare.slnx subiendo desde el directorio de los tests.");
    }

    /// <summary>
    /// Agrega una ruta al final del pipeline de la app (después de <c>next</c>), para que quede
    /// dentro del <c>UseExceptionHandler</c> que configura <c>program.cs</c>.
    /// </summary>
    private sealed class RutaDeTest(string ruta, RequestDelegate manejador) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Map(ruta, rama => rama.Run(manejador));
        };
    }
}
