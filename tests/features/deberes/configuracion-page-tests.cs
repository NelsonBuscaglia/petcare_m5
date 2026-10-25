using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PetCare.Shared.Data;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.Deberes;

public sealed class ConfiguracionPageTests : IDisposable
{
    private const string RutaFirulais = "/mascotas/1/deberes";
    private const string CampoToken = "__RequestVerificationToken";

    private readonly PetCareAppFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task GetConfiguracion_MascotaExistente_200ConFormulario()
    {
        using var cliente = CrearCliente(factory);

        using var respuesta = await cliente.GetAsync(RutaFirulais);
        var documento = await Parsear(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Firulais", Texto(documento, "nombre-mascota"));
        Assert.Empty(documento.QuerySelectorAll("[data-testid=deber-activo]"));
        var formulario = Assert.Single(documento.QuerySelectorAll("form[data-testid=form-alta]"));
        Assert.Equal("post", formulario.GetAttribute("method"), ignoreCase: true);
        Assert.Equal(RutaFirulais, formulario.GetAttribute("hx-post"));
        Assert.Equal("outerHTML", formulario.GetAttribute("hx-swap"));
        Assert.Equal(
            ["comida", "paseo"],
            formulario.QuerySelectorAll("input[type=radio][name=tipo]").Select(e => e.GetAttribute("value")));
        var hora = Assert.Single(formulario.QuerySelectorAll("input[type=time][name=hora]"));
        Assert.True(string.IsNullOrEmpty(hora.GetAttribute("value")));
        Assert.Equal(
            ["solo-hoy", "todos-los-dias"],
            formulario.QuerySelectorAll("input[type=radio][name=repeticion]").Select(e => e.GetAttribute("value")));
        Assert.Empty(formulario.QuerySelectorAll("input[type=radio][checked]"));
        Assert.Single(formulario.QuerySelectorAll($"input[type=hidden][name={CampoToken}]"));
        var volver = Assert.Single(documento.QuerySelectorAll("a[data-testid=volver]"));
        Assert.Equal("/", volver.GetAttribute("href"));
        Assert.Equal("Volver al estado del día", volver.TextContent.Trim());
    }

    [Fact]
    public async Task GetConfiguracion_MascotaInexistente_404()
    {
        using var cliente = CrearCliente(factory);

        using var respuesta = await cliente.GetAsync("/mascotas/999/deberes");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.DoesNotContain("Firulais", cuerpo);
    }

    [Fact]
    public async Task PostAlta_Htmx_DevuelveParcialConDeber()
    {
        using var cliente = CrearCliente(factory);
        var token = await ObtenerToken(cliente);

        using var respuesta = await Postear(cliente, RutaFirulais, token, "paseo", "18:00", "todos-los-dias", htmx: true);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        var documento = new HtmlParser().ParseDocument(cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.DoesNotContain("<html", cuerpo, StringComparison.OrdinalIgnoreCase);
        var contenedor = Assert.Single(documento.QuerySelectorAll("[data-testid=configuracion-deberes]"));
        Assert.Equal("#" + contenedor.Id, documento.QuerySelector("form[data-testid=form-alta]")?.GetAttribute("hx-target"));
        Assert.Equal(["Paseo · 18:00 · todos los días"], TextosDeDeberes(documento));
        Assert.Empty(documento.QuerySelectorAll("[data-testid=error]"));
        Assert.True(string.IsNullOrEmpty(documento.QuerySelector("input[name=hora]")?.GetAttribute("value")));
        Assert.Empty(documento.QuerySelectorAll("input[type=radio][checked]"));
        Assert.Single(documento.QuerySelectorAll($"input[type=hidden][name={CampoToken}]"));
    }

    [Fact]
    public async Task PostAlta_SinHtmx_Redirige303()
    {
        using var cliente = CrearCliente(factory);
        var token = await ObtenerToken(cliente);

        using var respuesta = await Postear(cliente, RutaFirulais, token, "comida", "20:00", "solo-hoy");

        Assert.Equal(HttpStatusCode.SeeOther, respuesta.StatusCode);
        Assert.Equal(RutaFirulais, respuesta.Headers.Location?.OriginalString);
        using var pagina = await cliente.GetAsync(RutaFirulais);
        Assert.Equal(["Comida · 20:00 · solo para hoy"], TextosDeDeberes(await Parsear(pagina)));
    }

    [Fact]
    public async Task PostAlta_SinHora_MuestraMensajeYNoCrea()
    {
        await AltaRechazadaMuestraMensaje("comida", "", "solo-hoy", "La hora es obligatoria.");
    }

    [Fact]
    public async Task PostAlta_Hora2500_MuestraMensajeYNoCrea()
    {
        var documento = await AltaRechazadaMuestraMensaje(
            "comida", "25:00", "solo-hoy", "La hora no es válida: usá el formato HH:MM, entre 00:00 y 23:59.");

        Assert.Equal("25:00", documento.QuerySelector("input[name=hora]")?.GetAttribute("value"));
        Assert.Equal("comida", documento.QuerySelector("input[name=tipo][checked]")?.GetAttribute("value"));
        Assert.Equal("solo-hoy", documento.QuerySelector("input[name=repeticion][checked]")?.GetAttribute("value"));
    }

    [Fact]
    public async Task PostAlta_TipoMedicacion_MuestraMensajeYNoCrea()
    {
        await AltaRechazadaMuestraMensaje("medicación", "08:00", "solo-hoy", "El tipo debe ser comida o paseo.");
    }

    [Fact]
    public async Task PostAlta_SinRepeticion_MuestraMensajeYNoCrea()
    {
        await AltaRechazadaMuestraMensaje("comida", "08:00", null, "Elegí \"solo para hoy\" o \"todos los días\".");
    }

    [Fact]
    public async Task PostAlta_MascotaInexistente_404()
    {
        using var cliente = CrearCliente(factory);
        var token = await ObtenerToken(cliente);

        using var respuesta = await Postear(cliente, "/mascotas/999/deberes", token, "paseo", "18:00", "todos-los-dias");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        await using var contexto = factory.CrearContexto();
        Assert.Equal(0, await contexto.Deberes.CountAsync());
    }

    [Fact]
    public async Task PostAlta_SinTokenAntiforgery_400()
    {
        using var cliente = CrearCliente(factory);
        var token = await ObtenerToken(cliente);

        using var sinToken = await Postear(cliente, RutaFirulais, token: null, "paseo", "18:00", "todos-los-dias");
        using var tokenInvalido = await Postear(cliente, RutaFirulais, "no-es-un-token", "paseo", "18:00", "todos-los-dias");

        Assert.Equal(HttpStatusCode.BadRequest, sinToken.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tokenInvalido.StatusCode);
        await using (var contexto = factory.CrearContexto())
        {
            Assert.Equal(0, await contexto.Deberes.CountAsync());
        }

        // Control: la misma alta con el token válido se acepta, así que el 400 lo causó el token.
        using var conToken = await Postear(cliente, RutaFirulais, token, "paseo", "18:00", "todos-los-dias");
        Assert.Equal(HttpStatusCode.SeeOther, conToken.StatusCode);
    }

    [Fact]
    public async Task PostAlta_FallaDeBase_Devuelve500Generico()
    {
        using var conFalla = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.ConfigureServices(s => s.ConfigureDbContext<PetCareDbContext>(
                o => o.AddInterceptors(new FallaAlGuardar())));
        });
        using var cliente = CrearCliente(conFalla);
        var token = await ObtenerToken(cliente);

        using var respuesta = await Postear(cliente, RutaFirulais, token, "paseo", "18:00", "todos-los-dias");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Contains("Ocurrió un error inesperado.", new HtmlParser().ParseDocument(cuerpo).Body?.TextContent ?? "");
        Assert.DoesNotContain(FallaAlGuardar.Detalle, cuerpo);
        await using var contexto = factory.CrearContexto();
        Assert.Equal(0, await contexto.Deberes.CountAsync());
        Assert.Equal(0, await contexto.Instancias.CountAsync());
    }

    [Fact]
    public async Task PostAlta_TipoConHtml_SeMuestraCodificado()
    {
        using var cliente = CrearCliente(factory);
        var token = await ObtenerToken(cliente);

        using var respuesta = await Postear(cliente, RutaFirulais, token, "<script>", "08:00", "solo-hoy", htmx: true);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.DoesNotContain("<script>", cuerpo, StringComparison.OrdinalIgnoreCase);
        var documento = new HtmlParser().ParseDocument(cuerpo);
        Assert.Equal(["El tipo debe ser comida o paseo."], Errores(documento));
        Assert.Empty(documento.QuerySelectorAll("script"));
    }

    private async Task<IDocument> AltaRechazadaMuestraMensaje(
        string? tipo, string? hora, string? repeticion, string mensajeEsperado)
    {
        using var cliente = CrearCliente(factory);
        var token = await ObtenerToken(cliente);

        using var respuesta = await Postear(cliente, RutaFirulais, token, tipo, hora, repeticion);
        var documento = await Parsear(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Firulais", Texto(documento, "nombre-mascota"));
        Assert.Equal([mensajeEsperado], Errores(documento));
        Assert.Empty(documento.QuerySelectorAll("[data-testid=deber-activo]"));
        await using var contexto = factory.CrearContexto();
        Assert.Equal(0, await contexto.Deberes.CountAsync());
        Assert.Equal(0, await contexto.Instancias.CountAsync());
        return documento;
    }

    private static HttpClient CrearCliente(WebApplicationFactory<Program> fabrica) =>
        fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>GET de la página para obtener la cookie y el token antiforgery del formulario.</summary>
    private static async Task<string> ObtenerToken(HttpClient cliente)
    {
        using var respuesta = await cliente.GetAsync(RutaFirulais);
        var documento = await Parsear(respuesta);
        return documento.QuerySelector($"input[name={CampoToken}]")?.GetAttribute("value")
            ?? throw new InvalidOperationException("La página no tiene token antiforgery.");
    }

    private static Task<HttpResponseMessage> Postear(
        HttpClient cliente,
        string ruta,
        string? token,
        string? tipo,
        string? hora,
        string? repeticion,
        bool htmx = false)
    {
        var campos = new List<KeyValuePair<string, string>>();
        Agregar(campos, CampoToken, token);
        Agregar(campos, "tipo", tipo);
        Agregar(campos, "hora", hora);
        Agregar(campos, "repeticion", repeticion);
        var pedido = new HttpRequestMessage(HttpMethod.Post, ruta) { Content = new FormUrlEncodedContent(campos) };
        if (htmx)
        {
            pedido.Headers.Add("HX-Request", "true");
        }

        return cliente.SendAsync(pedido);
    }

    private static void Agregar(List<KeyValuePair<string, string>> campos, string nombre, string? valor)
    {
        if (valor is not null)
        {
            campos.Add(new(nombre, valor));
        }
    }

    private static async Task<IDocument> Parsear(HttpResponseMessage respuesta) =>
        new HtmlParser().ParseDocument(await respuesta.Content.ReadAsStringAsync());

    private static string? Texto(IDocument documento, string testId) =>
        documento.QuerySelector($"[data-testid={testId}]")?.TextContent.Trim();

    private static List<string> TextosDeDeberes(IDocument documento) =>
        documento.QuerySelectorAll("[data-testid=deber-activo]").Select(e => e.TextContent.Trim()).ToList();

    private static List<string> Errores(IDocument documento) =>
        documento.QuerySelectorAll("[data-testid=error]").Select(e => e.TextContent.Trim()).ToList();

    /// <summary>Simula una falla de la base al guardar (por ejemplo, disco lleno o base bloqueada).</summary>
    private sealed class FallaAlGuardar : SaveChangesInterceptor
    {
        public const string Detalle = "detalle-secreto-de-la-falla-de-base";

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
            throw new DbUpdateException(Detalle);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new DbUpdateException(Detalle);
    }
}
