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
using PetCare.Shared.Data;
using PetCare.Shared.Tiempo;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.EstadoDelDia;

public sealed class CumplimientoPageTests : IDisposable
{
    private const int Firulais = 1;
    private const int Michi = 2;
    private const string CampoToken = "__RequestVerificationToken";
    private const string RutaCumplir = "/?handler=Cumplir";

    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Manana = Hoy.AddDays(1);

    private readonly PetCareAppFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task FormularioCumplimiento_A1030_ProponeHoraActual()
    {
        await Alta(Firulais, "comida", "12:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        factory.Reloj.SetUtcNow(ZonaHogar.AUtc(Hoy.ToDateTime(new TimeOnly(10, 30, 40))));
        using var cliente = CrearCliente(factory);

        using var respuesta = await GetFormulario(cliente, instanciaId, htmx: true);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        var documento = new HtmlParser().ParseDocument(cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.DoesNotContain("<html", cuerpo, StringComparison.OrdinalIgnoreCase);
        var contenedor = Assert.Single(documento.QuerySelectorAll("#estado-del-dia"));
        var instancia = Assert.Single(contenedor.QuerySelectorAll($"[data-testid=instancia][data-instancia-id='{instanciaId}']"));
        var formulario = Assert.Single(instancia.QuerySelectorAll("form[data-formulario-cumplimiento]"));
        Assert.Equal("post", formulario.GetAttribute("method"), ignoreCase: true);
        Assert.Equal(RutaCumplir, formulario.GetAttribute("action"));
        Assert.Equal(RutaCumplir, formulario.GetAttribute("hx-post"));
        Assert.Equal("#estado-del-dia", formulario.GetAttribute("hx-target"));
        Assert.Equal("outerHTML", formulario.GetAttribute("hx-swap"));
        Assert.Equal("closest #estado-del-dia:replace", formulario.GetAttribute("hx-sync"));
        Assert.Equal(
            instanciaId.ToString(CultureInfo.InvariantCulture),
            formulario.QuerySelector("input[type=hidden][name=instanciaId]")?.GetAttribute("value"));
        var hora = Assert.Single(formulario.QuerySelectorAll("input[type=time][name=horaReal][data-testid=hora-real]"));
        Assert.Equal("10:30", hora.GetAttribute("value"));
        Assert.Single(formulario.QuerySelectorAll($"input[type=hidden][name={CampoToken}]"));
        Assert.Empty(formulario.QuerySelectorAll("[data-testid=error-cumplimiento]"));
        Assert.Equal("Confirmar", formulario.QuerySelector("button[data-testid=confirmar]")?.TextContent.Trim());
        var cancelar = Assert.Single(formulario.QuerySelectorAll("a[data-testid=cancelar]"));
        Assert.Equal("/", cancelar.GetAttribute("href"));
        Assert.Equal("/?handler=Estado", cancelar.GetAttribute("hx-get"));
        Assert.Equal("Cancelar", cancelar.TextContent.Trim());

        // Sin HTMX: la página completa, con el formulario abierto en la misma instancia.
        using var sinHtmx = await GetFormulario(cliente, instanciaId, htmx: false);
        var cuerpoSinHtmx = await sinHtmx.Content.ReadAsStringAsync();
        var pagina = new HtmlParser().ParseDocument(cuerpoSinHtmx);
        Assert.Equal(HttpStatusCode.OK, sinHtmx.StatusCode);
        Assert.Contains("<html", cuerpoSinHtmx, StringComparison.OrdinalIgnoreCase);
        var instanciaEnPagina = Assert.Single(pagina.QuerySelectorAll($"#estado-del-dia [data-testid=instancia][data-instancia-id='{instanciaId}']"));
        Assert.Equal(
            "10:30",
            Assert.Single(instanciaEnPagina.QuerySelectorAll("form[data-formulario-cumplimiento] [data-testid=hora-real]")).GetAttribute("value"));
    }

    [Fact]
    public async Task FormularioCumplimiento_InstanciaInexistente_404()
    {
        using var cliente = CrearCliente(factory);

        using var htmx = await GetFormulario(cliente, 999, htmx: true);
        using var sinHtmx = await GetFormulario(cliente, 999, htmx: false);

        Assert.Equal(HttpStatusCode.NotFound, htmx.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, sinHtmx.StatusCode);
    }

    [Fact]
    public async Task FormularioCumplimiento_InstanciaCumplida_MuestraMensaje()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        factory.Base.InsertarRegistro(instanciaId, Hoy, new TimeOnly(9, 0));
        using var cliente = CrearCliente(factory);

        using var respuesta = await GetFormulario(cliente, instanciaId, htmx: true);
        var documento = await Parsear(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var contenedor = Assert.Single(documento.QuerySelectorAll("#estado-del-dia"));
        Assert.Equal("La instancia ya fue cumplida.", Texto(contenedor, "mensaje"));
        Assert.Empty(contenedor.QuerySelectorAll("form[data-formulario-cumplimiento]"));
    }

    [Fact]
    public async Task FormularioCumplimiento_SinInstanciaId_400()
    {
        using var cliente = CrearCliente(factory);

        using var sinId = await cliente.GetAsync("/?handler=FormularioCumplimiento");
        using var noEntero = await cliente.GetAsync("/?handler=FormularioCumplimiento&instanciaId=abc");

        Assert.Equal(HttpStatusCode.BadRequest, sinId.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noEntero.StatusCode);
    }

    [Fact]
    public async Task Cumplir_Htmx_InstanciaDesapareceYSumaPaseo()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var paseoId = await IdDeInstancia();
        await Alta(Firulais, "comida", "20:00", "solo-hoy");
        using var cliente = CrearCliente(factory);
        var token = await TokenDelFormulario(cliente, paseoId);
        Assert.Equal("0 paseos", Texto(Seccion(await GetIndex(cliente), Firulais), "paseos-hoy"));

        using var respuesta = await Cumplir(cliente, token, paseoId, "09:00", htmx: true);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        var documento = new HtmlParser().ParseDocument(cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.DoesNotContain("<html", cuerpo, StringComparison.OrdinalIgnoreCase);
        var contenedor = Assert.Single(documento.QuerySelectorAll("#estado-del-dia"));
        var seccion = Assert.Single(contenedor.QuerySelectorAll($"section[data-testid=seccion-{Firulais}]"));
        Assert.Equal(["Comida · 20:00"], TextosDeInstancias(seccion));
        Assert.Empty(seccion.QuerySelectorAll($"[data-instancia-id='{paseoId}']"));
        Assert.Equal("1 paseo", Texto(seccion, "paseos-hoy"));
        Assert.Empty(contenedor.QuerySelectorAll("form[data-formulario-cumplimiento]"));
        Assert.Empty(contenedor.QuerySelectorAll("[data-testid=mensaje]"));
    }

    [Fact]
    public async Task Cumplir_SinHtmx_Redirige303()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        using var cliente = CrearCliente(factory);
        var token = await TokenDelFormulario(cliente, instanciaId);

        using var respuesta = await Cumplir(cliente, token, instanciaId, "09:00");

        Assert.Equal(HttpStatusCode.SeeOther, respuesta.StatusCode);
        Assert.Equal("/", respuesta.Headers.Location?.OriginalString);
        Assert.Equal(1, await CantidadDeRegistros());
        var seccion = Seccion(await GetIndex(cliente), Firulais);
        Assert.Empty(TextosDeInstancias(seccion));
        Assert.Equal("1 paseo", Texto(seccion, "paseos-hoy"));
    }

    [Fact]
    public async Task Cumplir_AceptandoHoraPropuesta_UltimaComida1030()
    {
        await Alta(Firulais, "comida", "12:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        IrALocal(Hoy, 10, 30);
        using var cliente = CrearCliente(factory);
        using var formulario = await GetFormulario(cliente, instanciaId, htmx: true);
        var documentoFormulario = await Parsear(formulario);
        var horaPropuesta = documentoFormulario.QuerySelector("[data-testid=hora-real]")?.GetAttribute("value");
        var token = documentoFormulario.QuerySelector($"input[name={CampoToken}]")?.GetAttribute("value");
        Assert.Equal("10:30", horaPropuesta);

        using var respuesta = await Cumplir(cliente, token, instanciaId, horaPropuesta, htmx: true);
        var documento = await Parsear(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Última comida: 25/10/2026 10:30", Texto(Seccion(documento, Firulais), "ultima-comida"));
    }

    [Fact]
    public async Task Cumplir_Hora1000_UltimaComida1000()
    {
        await Alta(Firulais, "comida", "12:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        IrALocal(Hoy, 10, 30);
        using var cliente = CrearCliente(factory);
        var token = await TokenDelFormulario(cliente, instanciaId);

        using var respuesta = await Cumplir(cliente, token, instanciaId, "10:00", htmx: true);
        var documento = await Parsear(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Última comida: 25/10/2026 10:00", Texto(Seccion(documento, Firulais), "ultima-comida"));
    }

    [Fact]
    public async Task Cumplir_Hora1100_RechazaYMantieneInstancia()
    {
        await Alta(Firulais, "comida", "12:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        IrALocal(Hoy, 10, 30);
        using var cliente = CrearCliente(factory);
        var token = await TokenDelFormulario(cliente, instanciaId);

        using var respuesta = await Cumplir(cliente, token, instanciaId, "11:00", htmx: true);
        var documento = await Parsear(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var seccion = Seccion(documento, Firulais);
        var instancia = Assert.Single(seccion.QuerySelectorAll($"[data-testid=instancia][data-instancia-id='{instanciaId}']"));
        var formulario = Assert.Single(instancia.QuerySelectorAll("form[data-formulario-cumplimiento]"));
        Assert.Equal("La hora no puede ser posterior a la actual.", Texto(formulario, "error-cumplimiento"));
        Assert.Equal("11:00", formulario.QuerySelector("[data-testid=hora-real]")?.GetAttribute("value"));
        Assert.Single(formulario.QuerySelectorAll($"input[type=hidden][name={CampoToken}]"));
        Assert.Equal("Sin comidas registradas", Texto(seccion, "ultima-comida"));
        Assert.Equal(0, await CantidadDeRegistros());

        // Sin HTMX: 200 con la página completa y el formulario abierto con el error.
        using var sinHtmx = await Cumplir(cliente, token, instanciaId, "11:00");
        var cuerpoSinHtmx = await sinHtmx.Content.ReadAsStringAsync();
        var pagina = new HtmlParser().ParseDocument(cuerpoSinHtmx);
        Assert.Equal(HttpStatusCode.OK, sinHtmx.StatusCode);
        Assert.Contains("<html", cuerpoSinHtmx, StringComparison.OrdinalIgnoreCase);
        var formularioEnPagina = Assert.Single(pagina.QuerySelectorAll("#estado-del-dia form[data-formulario-cumplimiento]"));
        Assert.Equal("La hora no puede ser posterior a la actual.", Texto(formularioEnPagina, "error-cumplimiento"));
        Assert.Equal(0, await CantidadDeRegistros());
    }

    [Fact]
    public async Task Cumplir_DosVeces_SegundoMuestraYaCumplida()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        using var cliente = CrearCliente(factory);
        var token = await TokenDelFormulario(cliente, instanciaId);
        using var primero = await Cumplir(cliente, token, instanciaId, "09:00", htmx: true);
        Assert.Equal(HttpStatusCode.OK, primero.StatusCode);

        using var segundo = await Cumplir(cliente, token, instanciaId, "09:00", htmx: true);
        var documento = await Parsear(segundo);

        Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);
        var contenedor = Assert.Single(documento.QuerySelectorAll("#estado-del-dia"));
        Assert.Equal("La instancia ya fue cumplida.", Texto(contenedor, "mensaje"));
        Assert.Empty(contenedor.QuerySelectorAll("form[data-formulario-cumplimiento]"));
        Assert.Equal("1 paseo", Texto(Seccion(documento, Firulais), "paseos-hoy"));
        Assert.Equal(1, await CantidadDeRegistros());

        // Sin HTMX: el rechazo es 200 con la página completa y el mensaje (no 303).
        using var sinHtmx = await Cumplir(cliente, token, instanciaId, "09:00");
        var cuerpoSinHtmx = await sinHtmx.Content.ReadAsStringAsync();
        var pagina = new HtmlParser().ParseDocument(cuerpoSinHtmx);
        Assert.Equal(HttpStatusCode.OK, sinHtmx.StatusCode);
        Assert.Contains("<html", cuerpoSinHtmx, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("La instancia ya fue cumplida.", Texto(Assert.Single(pagina.QuerySelectorAll("#estado-del-dia")), "mensaje"));
        Assert.Equal(1, await CantidadDeRegistros());
    }

    [Fact]
    public async Task Cumplir_TrasMedianoche_MuestraExpiro()
    {
        await Alta(Firulais, "paseo", "20:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        IrALocal(Hoy, 23, 59);
        using var cliente = CrearCliente(factory);
        using var formulario = await GetFormulario(cliente, instanciaId, htmx: true);
        var documentoFormulario = await Parsear(formulario);
        Assert.Single(documentoFormulario.QuerySelectorAll("form[data-formulario-cumplimiento]"));
        var token = documentoFormulario.QuerySelector($"input[name={CampoToken}]")?.GetAttribute("value");
        IrALocal(Manana, 0, 1);

        using var respuesta = await Cumplir(cliente, token, instanciaId, "23:59", htmx: true);
        var documento = await Parsear(respuesta);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var contenedor = Assert.Single(documento.QuerySelectorAll("#estado-del-dia"));
        Assert.Equal("La instancia expiró.", Texto(contenedor, "mensaje"));
        Assert.Empty(contenedor.QuerySelectorAll("form[data-formulario-cumplimiento]"));
        Assert.Empty(TextosDeInstancias(Seccion(documento, Firulais)));
        Assert.Equal(0, await CantidadDeRegistros());
    }

    [Fact]
    public async Task Cumplir_SoloHoy_DejaDeAparecerEnConfiguracion()
    {
        await Alta(Firulais, "paseo", "09:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        using var cliente = CrearCliente(factory);
        Assert.Equal(["Paseo · 09:00 · solo para hoy"], await DeberesActivos(cliente));
        var token = await TokenDelFormulario(cliente, instanciaId);

        using var respuesta = await Cumplir(cliente, token, instanciaId, "09:00");

        Assert.Equal(HttpStatusCode.SeeOther, respuesta.StatusCode);
        Assert.Empty(await DeberesActivos(cliente));
    }

    [Fact]
    public async Task Cumplir_UnicoDeber_MuestraSinTareasYUnPaseo()
    {
        await Alta(Firulais, "paseo", "09:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        IrALocal(Hoy, 9, 5);
        using var cliente = CrearCliente(factory);
        var token = await TokenDelFormulario(cliente, instanciaId);
        using var respuesta = await Cumplir(cliente, token, instanciaId, "09:05");
        Assert.Equal(HttpStatusCode.SeeOther, respuesta.StatusCode);
        IrALocal(Hoy, 10, 0);

        var seccion = Seccion(await GetIndex(cliente), Firulais);

        Assert.Equal("1 paseo", Texto(seccion, "paseos-hoy"));
        Assert.Equal("Último paseo: 25/10/2026 09:05", Texto(seccion, "ultimo-paseo"));
        Assert.Equal("No hay tareas pendientes.", Texto(seccion, "sin-tareas"));
        Assert.Empty(TextosDeInstancias(seccion));
    }

    [Fact]
    public async Task Cumplir_InstanciaInexistente_404()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        using var cliente = CrearCliente(factory);
        var token = await TokenDelFormulario(cliente, instanciaId);

        using var htmx = await Cumplir(cliente, token, 999, "09:00", htmx: true);
        using var sinHtmx = await Cumplir(cliente, token, 999, "09:00");

        Assert.Equal(HttpStatusCode.NotFound, htmx.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, sinHtmx.StatusCode);
        Assert.Equal(0, await CantidadDeRegistros());
    }

    [Fact]
    public async Task Cumplir_SinInstanciaId_400()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        using var cliente = CrearCliente(factory);
        var token = await TokenDelFormulario(cliente, instanciaId);

        using var sinId = await Postear(cliente, token, instanciaId: null, "09:00", htmx: true);
        using var noEntero = await Postear(cliente, token, instanciaId: "abc", "09:00", htmx: true);

        Assert.Equal(HttpStatusCode.BadRequest, sinId.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noEntero.StatusCode);
        Assert.Equal(0, await CantidadDeRegistros());
    }

    [Fact]
    public async Task Cumplir_SinToken_400()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        using var cliente = CrearCliente(factory);
        var token = await TokenDelFormulario(cliente, instanciaId);

        using var sinToken = await Cumplir(cliente, token: null, instanciaId, "09:00", htmx: true);
        using var tokenInvalido = await Cumplir(cliente, "no-es-un-token", instanciaId, "09:00", htmx: true);

        Assert.Equal(HttpStatusCode.BadRequest, sinToken.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tokenInvalido.StatusCode);
        Assert.Equal(0, await CantidadDeRegistros());

        // Control: el mismo POST con el token válido registra, así que el 400 lo causó el token.
        using var conToken = await Cumplir(cliente, token, instanciaId, "09:00");
        Assert.Equal(HttpStatusCode.SeeOther, conToken.StatusCode);
        Assert.Equal(1, await CantidadDeRegistros());
    }

    [Fact]
    public async Task Cumplir_FallaDeBase_500Generico()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        using var conFalla = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.ConfigureServices(s => s.ConfigureDbContext<PetCareDbContext>(
                o => o.AddInterceptors(new FallaAlGuardar())));
        });
        using var cliente = CrearCliente(conFalla);
        var token = await TokenDelFormulario(cliente, instanciaId);

        using var respuesta = await Cumplir(cliente, token, instanciaId, "09:00", htmx: true);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Contains("Ocurrió un error inesperado.", new HtmlParser().ParseDocument(cuerpo).Body?.TextContent ?? "");
        Assert.DoesNotContain(FallaAlGuardar.Detalle, cuerpo);
        Assert.DoesNotContain("DbUpdateException", cuerpo);
        Assert.Equal(0, await CantidadDeRegistros());
    }

    [Fact]
    public async Task Cumplir_HoraConHtml_SeMuestraCodificado()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        using var cliente = CrearCliente(factory);
        var token = await TokenDelFormulario(cliente, instanciaId);

        using var respuesta = await Cumplir(cliente, token, instanciaId, "<b>", htmx: true);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        var documento = new HtmlParser().ParseDocument(cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.DoesNotContain("<b>", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(documento.QuerySelectorAll("b"));
        var formulario = Assert.Single(documento.QuerySelectorAll("form[data-formulario-cumplimiento]"));
        Assert.Equal(
            "La hora no es válida: usá el formato HH:MM, entre 00:00 y 23:59.",
            Texto(formulario, "error-cumplimiento"));
        Assert.Equal("<b>", formulario.QuerySelector("[data-testid=hora-real]")?.GetAttribute("value"));
        Assert.Equal(0, await CantidadDeRegistros());
    }

    [Fact]
    public async Task Estado_DevuelveParcialSinLayout()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        using var cliente = CrearCliente(factory);

        using var respuesta = await cliente.GetAsync("/?handler=Estado");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        var documento = new HtmlParser().ParseDocument(cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.DoesNotContain("<html", cuerpo, StringComparison.OrdinalIgnoreCase);
        var contenedor = Assert.Single(documento.QuerySelectorAll("#estado-del-dia"));
        Assert.Equal("estado-del-dia", contenedor.GetAttribute("data-testid"));
        Assert.Equal(["Paseo · 18:00"], TextosDeInstancias(Seccion(documento, Firulais)));
    }

    [Fact]
    public async Task Index_TestIdsDeFeat001a_SeMantienen()
    {
        await Alta(Firulais, "paseo", "08:00", "solo-hoy");
        var instanciaId = await IdDeInstancia();
        using var cliente = CrearCliente(factory);

        var documento = await GetIndex(cliente);

        var contenedor = Assert.Single(documento.QuerySelectorAll("#estado-del-dia[data-testid=estado-del-dia]"));
        Assert.Equal(
            ["seccion-1", "seccion-2"],
            contenedor.QuerySelectorAll("section[data-testid^=seccion-]").Select(s => s.GetAttribute("data-testid")));
        var firulais = Assert.Single(contenedor.QuerySelectorAll($"section[data-testid=seccion-{Firulais}]"));
        Assert.Equal("Firulais", Texto(firulais, "nombre-mascota"));
        var instancia = Assert.Single(firulais.QuerySelectorAll("[data-testid=instancia]"));
        Assert.Equal(instanciaId.ToString(CultureInfo.InvariantCulture), instancia.GetAttribute("data-instancia-id"));
        Assert.Equal("Paseo · 08:00", Texto(instancia, "texto-instancia"));
        Assert.Equal("pendiente", Texto(instancia, "marca-pendiente"));
        var cumplido = Assert.Single(instancia.QuerySelectorAll("a[data-testid=cumplido]"));
        var rutaFormulario = $"/?handler=FormularioCumplimiento&instanciaId={instanciaId}";
        Assert.Equal(rutaFormulario, cumplido.GetAttribute("href"));
        Assert.Equal(rutaFormulario, cumplido.GetAttribute("hx-get"));
        Assert.Equal("#estado-del-dia", cumplido.GetAttribute("hx-target"));
        Assert.Equal("outerHTML", cumplido.GetAttribute("hx-swap"));
        Assert.Equal("closest #estado-del-dia:replace", cumplido.GetAttribute("hx-sync"));
        Assert.Equal("Cumplido", cumplido.TextContent.Trim());
        Assert.Equal("0 paseos", Texto(firulais, "paseos-hoy"));
        Assert.Equal("Sin comidas registradas", Texto(firulais, "ultima-comida"));
        Assert.Equal("Sin paseos registrados", Texto(firulais, "ultimo-paseo"));
        Assert.Equal("/mascotas/1/deberes", firulais.QuerySelector("a[data-testid=configurar-deberes]")?.GetAttribute("href"));
        var michi = Assert.Single(contenedor.QuerySelectorAll($"section[data-testid=seccion-{Michi}]"));
        Assert.Equal("No hay tareas pendientes.", Texto(michi, "sin-tareas"));
        Assert.Empty(contenedor.QuerySelectorAll("[data-testid=mensaje]"));
        Assert.Empty(contenedor.QuerySelectorAll("form[data-formulario-cumplimiento]"));
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

    /// <summary>Id de la instancia creada por el último alta.</summary>
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

    /// <summary>
    /// Abre el formulario de cumplimiento (como HTMX) para obtener la cookie y el token
    /// antiforgery que trae el propio formulario.
    /// </summary>
    private static async Task<string> TokenDelFormulario(HttpClient cliente, int instanciaId)
    {
        using var respuesta = await GetFormulario(cliente, instanciaId, htmx: true);
        var documento = await Parsear(respuesta);
        return documento.QuerySelector($"form[data-formulario-cumplimiento] input[name={CampoToken}]")?.GetAttribute("value")
            ?? throw new InvalidOperationException("El formulario de cumplimiento no tiene token antiforgery.");
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

    private static Task<HttpResponseMessage> GetFormulario(HttpClient cliente, int instanciaId, bool htmx)
    {
        var pedido = new HttpRequestMessage(
            HttpMethod.Get,
            $"/?handler=FormularioCumplimiento&instanciaId={instanciaId.ToString(CultureInfo.InvariantCulture)}");
        if (htmx)
        {
            pedido.Headers.Add("HX-Request", "true");
        }

        return cliente.SendAsync(pedido);
    }

    private static Task<HttpResponseMessage> Cumplir(
        HttpClient cliente, string? token, int instanciaId, string? horaReal, bool htmx = false) =>
        Postear(cliente, token, instanciaId.ToString(CultureInfo.InvariantCulture), horaReal, htmx);

    private static Task<HttpResponseMessage> Postear(
        HttpClient cliente, string? token, string? instanciaId, string? horaReal, bool htmx)
    {
        var campos = new List<KeyValuePair<string, string>>();
        Agregar(campos, CampoToken, token);
        Agregar(campos, "instanciaId", instanciaId);
        Agregar(campos, "horaReal", horaReal);
        var pedido = new HttpRequestMessage(HttpMethod.Post, RutaCumplir) { Content = new FormUrlEncodedContent(campos) };
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

    private static IElement Seccion(IDocument documento, int mascotaId) =>
        Assert.Single(documento.QuerySelectorAll($"section[data-testid=seccion-{mascotaId}]"));

    private static List<string> TextosDeInstancias(IElement seccion) =>
        seccion.QuerySelectorAll("[data-testid=texto-instancia]").Select(e => e.TextContent.Trim()).ToList();

    private static string? Texto(IElement elemento, string testId) =>
        elemento.QuerySelector($"[data-testid={testId}]")?.TextContent.Trim();

    /// <summary>Simula una falla de la base al guardar (por ejemplo, disco lleno o base bloqueada).</summary>
    private sealed class FallaAlGuardar : SaveChangesInterceptor
    {
        public const string Detalle = "detalle-secreto-de-la-falla-al-guardar";

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
            throw new DbUpdateException(Detalle);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new DbUpdateException(Detalle);
    }
}
