using System.Data.Common;
using System.Globalization;
using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PetCare.Features.Deberes.Data;
using PetCare.Features.Instancias.Data;
using PetCare.Shared.Data;
using PetCare.Shared.Tiempo;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.EstadoDelDia;

public sealed class EliminacionPageTests : IDisposable
{
    private const int Firulais = 1;
    private const string CampoToken = "__RequestVerificationToken";
    private const string RutaEliminar = "/?handler=EliminarRegistro";

    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Manana = Hoy.AddDays(1);

    private readonly PetCareAppFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task EliminarRegistro_UnoDeDos_MuestraUnPaseo()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var paseo18 = await IdDeInstancia();
        await Alta(Firulais, "paseo", "20:00", "solo-hoy");
        var paseo20 = await IdDeInstancia();
        var registro18 = await Registrar(paseo18, "09:00");
        var registro20 = await Registrar(paseo20, "09:00");
        using var cliente = CrearCliente(factory);
        var antes = Seccion(await GetIndex(cliente), Firulais);
        Assert.Equal("2 paseos", Texto(antes, "paseos-hoy"));
        var token = TokenDeEliminar(antes);

        using var respuesta = await Eliminar(cliente, token, registro18, htmx: true);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        var documento = new HtmlParser().ParseDocument(cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.DoesNotContain("<html", cuerpo, StringComparison.OrdinalIgnoreCase);
        var contenedor = Assert.Single(documento.QuerySelectorAll("#estado-del-dia"));
        var seccion = Assert.Single(contenedor.QuerySelectorAll($"section[data-testid=seccion-{Firulais}]"));
        Assert.Equal("1 paseo", Texto(seccion, "paseos-hoy"));
        Assert.Equal([registro20], IdsDeRegistros(seccion));
        Assert.Equal(["Paseo · 18:00"], TextosDeInstancias(seccion));
        Assert.Empty(contenedor.QuerySelectorAll("[data-testid=mensaje]"));
        Assert.Equal(1, await CantidadDeRegistros());
    }

    [Fact]
    public async Task EliminarRegistro_ComidaCumplidaPorError_VuelvePendiente()
    {
        await Alta(Firulais, "comida", "08:00", "todos-los-dias");
        IrALocal(Manana, 7, 0);
        await GenerarDelDia();
        var comidaDeManana = await IdDeInstancia();
        IrALocal(Manana, 8, 10);
        var registro = await Registrar(comidaDeManana, "08:10");
        IrALocal(Manana, 10, 0);
        using var cliente = CrearCliente(factory);
        var antes = Seccion(await GetIndex(cliente), Firulais);
        Assert.Empty(TextosDeInstancias(antes));
        var token = TokenDeEliminar(antes);

        using var respuesta = await Eliminar(cliente, token, registro, htmx: true);
        var documento = await Parsear(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var seccion = Seccion(documento, Firulais);
        var instancia = Assert.Single(seccion.QuerySelectorAll($"[data-testid=instancia][data-instancia-id='{comidaDeManana}']"));
        Assert.Equal("Comida · 08:00", Texto(instancia, "texto-instancia"));
        Assert.Equal("pendiente", Texto(instancia, "marca-pendiente"));
        Assert.Empty(seccion.QuerySelectorAll("[data-testid=registro]"));
    }

    [Fact]
    public async Task EliminarRegistro_DeAyer_MuestraMensaje()
    {
        await Alta(Firulais, "paseo", "18:00", "todos-los-dias");
        var paseoDeAyer = await IdDeInstancia();
        IrALocal(Hoy, 18, 0);
        var registroDeAyer = await Registrar(paseoDeAyer, "18:00");
        IrALocal(Manana, 9, 0);
        await GenerarDelDia();
        using var cliente = CrearCliente(factory);
        // Sin registros de hoy no hay botón "Eliminar": el token sale del formulario de cumplimiento.
        var token = await TokenDelFormulario(cliente, await IdDeInstancia());

        using var respuesta = await Eliminar(cliente, token, registroDeAyer, htmx: true);
        var documento = await Parsear(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var contenedor = Assert.Single(documento.QuerySelectorAll("#estado-del-dia"));
        Assert.Equal("Solo se pueden eliminar registros del día.", Texto(contenedor, "mensaje"));
        var seccion = Seccion(documento, Firulais);
        Assert.Equal("Último paseo: 25/10/2026 18:00", Texto(seccion, "ultimo-paseo"));
        Assert.Equal("0 paseos", Texto(seccion, "paseos-hoy"));
        Assert.Equal(1, await CantidadDeRegistros());

        // Sin HTMX: el rechazo es 200 con la página completa y el mensaje.
        using var sinHtmx = await Eliminar(cliente, token, registroDeAyer);
        var cuerpoSinHtmx = await sinHtmx.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, sinHtmx.StatusCode);
        Assert.Contains("<html", cuerpoSinHtmx, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            "Solo se pueden eliminar registros del día.",
            Texto(Assert.Single(new HtmlParser().ParseDocument(cuerpoSinHtmx).QuerySelectorAll("#estado-del-dia")), "mensaje"));
        Assert.Equal(1, await CantidadDeRegistros());
    }

    [Fact]
    public async Task EliminarRegistro_YaEliminado_MuestraMensaje()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        var registro = await Registrar(instanciaId, "09:00");
        using var cliente = CrearCliente(factory);
        var token = TokenDeEliminar(Seccion(await GetIndex(cliente), Firulais));
        using var primero = await Eliminar(cliente, token, registro, htmx: true);
        Assert.Equal(HttpStatusCode.OK, primero.StatusCode);
        Assert.Empty((await Parsear(primero)).QuerySelectorAll("[data-testid=mensaje]"));

        using var segundo = await Eliminar(cliente, token, registro, htmx: true);
        var documento = await Parsear(segundo);

        Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);
        var contenedor = Assert.Single(documento.QuerySelectorAll("#estado-del-dia"));
        Assert.Equal("El registro ya fue eliminado.", Texto(contenedor, "mensaje"));
        var seccion = Seccion(documento, Firulais);
        Assert.Single(seccion.QuerySelectorAll($"[data-testid=instancia][data-instancia-id='{instanciaId}']"));
        Assert.Equal(["Paseo · 18:00"], TextosDeInstancias(seccion));
        Assert.Equal(0, await CantidadDeRegistros());
    }

    [Fact]
    public async Task EliminarRegistro_SinHtmx_Redirige303()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        var registro = await Registrar(instanciaId, "09:00");
        using var cliente = CrearCliente(factory);
        var token = TokenDeEliminar(Seccion(await GetIndex(cliente), Firulais));

        using var respuesta = await Eliminar(cliente, token, registro);

        Assert.Equal(HttpStatusCode.SeeOther, respuesta.StatusCode);
        Assert.Equal("/", respuesta.Headers.Location?.OriginalString);
        Assert.Equal(0, await CantidadDeRegistros());
        var seccion = Seccion(await GetIndex(cliente), Firulais);
        Assert.Equal(["Paseo · 18:00"], TextosDeInstancias(seccion));
        Assert.Equal("0 paseos", Texto(seccion, "paseos-hoy"));
    }

    [Fact]
    public async Task EliminarRegistro_SinRegistroId_400()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var registro = await Registrar(await IdDeInstancia(), "09:00");
        using var cliente = CrearCliente(factory);
        var token = TokenDeEliminar(Seccion(await GetIndex(cliente), Firulais));

        using var sinId = await Postear(cliente, token, registroId: null, htmx: true);
        using var noEntero = await Postear(cliente, token, registroId: "abc", htmx: true);

        Assert.Equal(HttpStatusCode.BadRequest, sinId.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noEntero.StatusCode);
        Assert.Equal(1, await CantidadDeRegistros());

        // Control: con el id, el mismo POST elimina.
        using var conId = await Eliminar(cliente, token, registro);
        Assert.Equal(HttpStatusCode.SeeOther, conId.StatusCode);
        Assert.Equal(0, await CantidadDeRegistros());
    }

    [Fact]
    public async Task EliminarRegistro_SinToken_400()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var registro = await Registrar(await IdDeInstancia(), "09:00");
        using var cliente = CrearCliente(factory);
        var token = TokenDeEliminar(Seccion(await GetIndex(cliente), Firulais));

        using var sinToken = await Eliminar(cliente, token: null, registro, htmx: true);
        using var tokenInvalido = await Eliminar(cliente, "no-es-un-token", registro, htmx: true);

        Assert.Equal(HttpStatusCode.BadRequest, sinToken.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tokenInvalido.StatusCode);
        Assert.Equal(1, await CantidadDeRegistros());

        // Control: el mismo POST con el token válido elimina, así que el 400 lo causó el token.
        using var conToken = await Eliminar(cliente, token, registro);
        Assert.Equal(HttpStatusCode.SeeOther, conToken.StatusCode);
        Assert.Equal(0, await CantidadDeRegistros());
    }

    [Fact]
    public async Task EliminarRegistro_FallaDeBase_500Generico()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var registro = await Registrar(await IdDeInstancia(), "09:00");
        using var conFalla = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.ConfigureServices(s => s.ConfigureDbContext<PetCareDbContext>(
                o => o.AddInterceptors(new FallaAlEscribir())));
        });
        using var cliente = CrearCliente(conFalla);
        var token = TokenDeEliminar(Seccion(await GetIndex(cliente), Firulais));

        using var respuesta = await Eliminar(cliente, token, registro, htmx: true);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Contains("Ocurrió un error inesperado.", new HtmlParser().ParseDocument(cuerpo).Body?.TextContent ?? "");
        Assert.DoesNotContain(FallaAlEscribir.Detalle, cuerpo);
        Assert.DoesNotContain("InvalidOperationException", cuerpo);
        Assert.Equal(1, await CantidadDeRegistros());
    }

    [Fact]
    public async Task EliminarRegistro_DeberSoloHoy_VuelveAConfiguracion()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var registro = await Registrar(await IdDeInstancia(), "09:00");
        using var cliente = CrearCliente(factory);
        Assert.Empty(await DeberesActivos(cliente));
        var token = TokenDeEliminar(Seccion(await GetIndex(cliente), Firulais));

        using var respuesta = await Eliminar(cliente, token, registro);

        Assert.Equal(HttpStatusCode.SeeOther, respuesta.StatusCode);
        Assert.Equal(["Paseo · 18:00 · solo para hoy"], await DeberesActivos(cliente));
        await using var contexto = factory.CrearContexto();
        Assert.Equal(1, await contexto.Deberes.CountAsync());
    }

    /// <summary>Mueve el reloj a una hora local del hogar (UTC−3).</summary>
    private void IrALocal(DateOnly dia, int hora, int minuto) =>
        factory.Reloj.SetUtcNow(ZonaHogar.AUtc(dia.ToDateTime(new TimeOnly(hora, minuto))));

    private async Task Alta(int mascotaId, string tipo, string hora, string repeticion)
    {
        using var scope = factory.Services.CreateScope();
        var deberes = scope.ServiceProvider.GetRequiredService<DeberesService>();
        var resultado = await deberes.DarDeAltaAsync(mascotaId, tipo, hora, repeticion, CancellationToken.None);
        Assert.True(resultado.EsOk, $"El alta de {tipo} {hora} {repeticion} fue rechazada.");
    }

    private async Task GenerarDelDia()
    {
        using var scope = factory.Services.CreateScope();
        var resultado = await scope.ServiceProvider.GetRequiredService<InstanciasService>().GenerarDelDiaAsync(CancellationToken.None);
        Assert.True(resultado.EsOk);
    }

    /// <summary>Registra el cumplimiento como lo haría otro cuidador; devuelve el id del registro.</summary>
    private async Task<int> Registrar(int instanciaId, string horaReal)
    {
        using var scope = factory.Services.CreateScope();
        var resultado = await scope.ServiceProvider.GetRequiredService<RegistrosService>()
            .RegistrarAsync(instanciaId, horaReal, CancellationToken.None);
        Assert.True(resultado.EsOk, $"El registro de la instancia {instanciaId} a las {horaReal} fue rechazado.");
        return resultado.Valor;
    }

    /// <summary>Id de la última instancia creada.</summary>
    private async Task<int> IdDeInstancia()
    {
        await using var contexto = factory.CrearContexto();
        return await contexto.Instancias.OrderByDescending(i => i.Id).Select(i => i.Id).FirstAsync();
    }

    private async Task<int> CantidadDeRegistros()
    {
        await using var contexto = factory.CrearContexto();
        return await contexto.RegistrosCumplimiento.CountAsync();
    }

    private static HttpClient CrearCliente(WebApplicationFactory<Program> fabrica) =>
        fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Token antiforgery del formulario "Eliminar" del primer registro de la sección.</summary>
    private static string TokenDeEliminar(IElement seccion)
    {
        var tokens = seccion.QuerySelectorAll($"[data-testid=registro] form input[name={CampoToken}]");
        Assert.NotEmpty(tokens);
        return tokens[0].GetAttribute("value") ?? "";
    }

    /// <summary>Token antiforgery del formulario de cumplimiento de una instancia vigente.</summary>
    private static async Task<string> TokenDelFormulario(HttpClient cliente, int instanciaId)
    {
        var pedido = new HttpRequestMessage(
            HttpMethod.Get,
            $"/?handler=FormularioCumplimiento&instanciaId={instanciaId.ToString(CultureInfo.InvariantCulture)}");
        pedido.Headers.Add("HX-Request", "true");
        using var respuesta = await cliente.SendAsync(pedido);
        var documento = await Parsear(respuesta);
        return Assert.Single(documento.QuerySelectorAll($"form[data-formulario-cumplimiento] input[name={CampoToken}]"))
            .GetAttribute("value") ?? "";
    }

    private static async Task<IDocument> GetIndex(HttpClient cliente)
    {
        using var respuesta = await cliente.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await Parsear(respuesta);
    }

    private static async Task<List<string>> DeberesActivos(HttpClient cliente)
    {
        using var respuesta = await cliente.GetAsync("/mascotas/1/deberes");
        var documento = await Parsear(respuesta);
        return documento.QuerySelectorAll("[data-testid=deber-activo]").Select(e => e.TextContent.Trim()).ToList();
    }

    private static Task<HttpResponseMessage> Eliminar(HttpClient cliente, string? token, int registroId, bool htmx = false) =>
        Postear(cliente, token, registroId.ToString(CultureInfo.InvariantCulture), htmx);

    private static Task<HttpResponseMessage> Postear(HttpClient cliente, string? token, string? registroId, bool htmx)
    {
        var campos = new List<KeyValuePair<string, string>>();
        if (token is not null)
        {
            campos.Add(new(CampoToken, token));
        }

        if (registroId is not null)
        {
            campos.Add(new("registroId", registroId));
        }

        var pedido = new HttpRequestMessage(HttpMethod.Post, RutaEliminar) { Content = new FormUrlEncodedContent(campos) };
        if (htmx)
        {
            pedido.Headers.Add("HX-Request", "true");
        }

        return cliente.SendAsync(pedido);
    }

    private static async Task<IDocument> Parsear(HttpResponseMessage respuesta) =>
        new HtmlParser().ParseDocument(await respuesta.Content.ReadAsStringAsync());

    private static IElement Seccion(IDocument documento, int mascotaId) =>
        Assert.Single(documento.QuerySelectorAll($"section[data-testid=seccion-{mascotaId}]"));

    private static List<string> TextosDeInstancias(IElement seccion) =>
        seccion.QuerySelectorAll("[data-testid=texto-instancia]").Select(e => e.TextContent.Trim()).ToList();

    private static List<int> IdsDeRegistros(IElement seccion) =>
        seccion.QuerySelectorAll("[data-testid=registro]")
            .Select(r => int.Parse(r.GetAttribute("data-registro-id") ?? "", CultureInfo.InvariantCulture))
            .ToList();

    private static string? Texto(IElement elemento, string testId) =>
        elemento.QuerySelector($"[data-testid={testId}]")?.TextContent.Trim();

    /// <summary>Simula una falla de la base al escribir (por ejemplo, disco lleno o base bloqueada).</summary>
    private sealed class FallaAlEscribir : DbCommandInterceptor
    {
        public const string Detalle = "detalle-secreto-de-la-falla-al-escribir";

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result) =>
            throw new InvalidOperationException(Detalle);

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(Detalle);
    }
}
