using System.Data.Common;
using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PetCare.Features.Deberes.Data;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Hogar.Domain;
using PetCare.Features.Instancias.Data;
using PetCare.Shared.Data;
using PetCare.Shared.Tiempo;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.EstadoDelDia;

public sealed class IndexPageTests : IDisposable
{
    private const int Firulais = 1;
    private const int Michi = 2;

    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Manana = Hoy.AddDays(1);

    private readonly PetCareAppFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task Index_PrimeraVez_MuestraDosSeccionesVacias()
    {
        var documento = await GetIndex();

        var secciones = documento.QuerySelectorAll("section[data-testid^=seccion-]");
        Assert.Equal(["seccion-1", "seccion-2"], secciones.Select(s => s.GetAttribute("data-testid")));
        Assert.Equal(["Firulais", "Michi"], secciones.Select(s => Texto(s, "nombre-mascota")));
        Assert.All(secciones, seccion =>
        {
            Assert.Empty(seccion.QuerySelectorAll("[data-testid=instancia]"));
            Assert.Equal("No hay tareas pendientes.", Texto(seccion, "sin-tareas"));
            Assert.Equal("0 paseos", Texto(seccion, "paseos-hoy"));
            Assert.Equal("Sin comidas registradas", Texto(seccion, "ultima-comida"));
            Assert.Equal("Sin paseos registrados", Texto(seccion, "ultimo-paseo"));
        });
        Assert.Equal(
            ["/mascotas/1/deberes", "/mascotas/2/deberes"],
            secciones.Select(s => s.QuerySelector("a[data-testid=configurar-deberes]")?.GetAttribute("href")));
        Assert.All(
            documento.QuerySelectorAll("a[data-testid=configurar-deberes]"),
            a => Assert.Equal("Configurar deberes", a.TextContent.Trim()));
    }

    [Fact]
    public async Task Index_DeberDeFirulais_NoApareceEnSeccionDeMichi()
    {
        IrALocal(Manana, 7, 0);
        await Alta(Firulais, "paseo", "09:00", "solo-hoy");

        var documento = await GetIndex();

        Assert.Equal([("Paseo · 09:00", false)], Instancias(documento, Firulais));
        Assert.Empty(Instancias(documento, Michi));
        Assert.Equal("No hay tareas pendientes.", Texto(Seccion(documento, Michi), "sin-tareas"));
    }

    [Fact]
    public async Task Index_AltaConHoraPasada_MuestraPendiente()
    {
        IrALocal(Hoy, 10, 30);
        await Alta(Firulais, "paseo", "10:00", "solo-hoy");

        var documento = await GetIndex();

        Assert.Equal([("Paseo · 10:00", true)], Instancias(documento, Firulais));
        Assert.Equal("pendiente", Texto(Seccion(documento, Firulais), "marca-pendiente"));
    }

    [Fact]
    public async Task Index_Manana0800_MuestraInstanciaDeTodosLosDias()
    {
        await Alta(Firulais, "paseo", "09:00", "todos-los-dias");
        IrALocal(Manana, 8, 0);
        await GenerarDelDia();

        var documento = await GetIndex();

        Assert.Equal([("Paseo · 09:00", false)], Instancias(documento, Firulais));
        Assert.Equal([await IdDeInstancia(Manana)], IdsDeInstancias(documento, Firulais));
    }

    [Fact]
    public async Task Index_Manana0800_NoMuestraSoloHoyDeAyer()
    {
        await Alta(Firulais, "comida", "20:00", "solo-hoy");
        Assert.Equal([("Comida · 20:00", false)], Instancias(await GetIndex(), Firulais));
        IrALocal(Manana, 8, 0);
        await GenerarDelDia();

        var documento = await GetIndex();

        Assert.Empty(Instancias(documento, Firulais));
        Assert.Equal("No hay tareas pendientes.", Texto(Seccion(documento, Firulais), "sin-tareas"));
    }

    [Fact]
    public async Task Index_0005_MuestraInstanciaDelNuevoDia()
    {
        await Alta(Firulais, "comida", "08:00", "todos-los-dias");
        factory.Reloj.SetUtcNow(new DateTimeOffset(2026, 10, 26, 3, 5, 0, TimeSpan.Zero));
        await GenerarDelDia();

        var documento = await GetIndex();

        Assert.Equal([("Comida · 08:00", false)], Instancias(documento, Firulais));
        Assert.Equal([await IdDeInstancia(Manana)], IdsDeInstancias(documento, Firulais));
    }

    [Fact]
    public async Task Index_2359_NoMuestraInstanciaDeManana()
    {
        await Alta(Firulais, "comida", "08:00", "todos-los-dias");
        factory.Reloj.SetUtcNow(new DateTimeOffset(2026, 10, 26, 2, 59, 0, TimeSpan.Zero));
        await GenerarDelDia();

        var documento = await GetIndex();

        Assert.Equal([("Comida · 08:00", true)], Instancias(documento, Firulais));
        Assert.Equal([await IdDeInstancia(Hoy)], IdsDeInstancias(documento, Firulais));
        await using var contexto = factory.CrearContexto();
        Assert.False(await contexto.Instancias.AnyAsync(i => i.Fecha == Manana));
    }

    [Fact]
    public async Task Index_0910_SinMarcaPendiente()
    {
        await Alta(Firulais, "comida", "09:00", "todos-los-dias");
        IrALocal(Hoy, 9, 10);

        var documento = await GetIndex();

        Assert.Equal([("Comida · 09:00", false)], Instancias(documento, Firulais));
        Assert.Empty(Seccion(documento, Firulais).QuerySelectorAll("[data-testid=marca-pendiente]"));
    }

    [Fact]
    public async Task Index_0915_MarcaPendiente()
    {
        await Alta(Firulais, "comida", "09:00", "todos-los-dias");
        IrALocal(Hoy, 9, 15);

        var documento = await GetIndex();

        Assert.Equal([("Comida · 09:00", true)], Instancias(documento, Firulais));
    }

    [Fact]
    public async Task Index_0700_NoMuestraAyerYMuestraHoySinMarca()
    {
        // "Ayer" es el 25/10 (día del alta, cuya instancia de las 08:00 no se cumplió); "hoy", el 26/10.
        await Alta(Firulais, "comida", "08:00", "todos-los-dias");
        IrALocal(Manana, 7, 0);
        await GenerarDelDia();

        var documento = await GetIndex();

        Assert.Equal([("Comida · 08:00", false)], Instancias(documento, Firulais));
        Assert.Equal([await IdDeInstancia(Manana)], IdsDeInstancias(documento, Firulais));
    }

    [Fact]
    public async Task Index_1200_ComidaPendientePaseoSinMarca()
    {
        await Alta(Firulais, "paseo", "18:00", "todos-los-dias");
        await Alta(Firulais, "comida", "08:00", "todos-los-dias");
        IrALocal(Hoy, 12, 0);

        var documento = await GetIndex();

        Assert.Equal([("Comida · 08:00", true), ("Paseo · 18:00", false)], Instancias(documento, Firulais));
        Assert.Empty(Instancias(documento, Michi));
    }

    [Fact]
    public async Task Index_0515Local_NoMarcaPendiente()
    {
        await Alta(Firulais, "comida", "08:00", "todos-los-dias");
        // 08:15 UTC = 05:15 en UTC−3 (el reloj no puede retroceder, así que es el día siguiente).
        factory.Reloj.SetUtcNow(new DateTimeOffset(2026, 10, 26, 8, 15, 0, TimeSpan.Zero));
        await GenerarDelDia();

        var documento = await GetIndex();

        Assert.Equal([("Comida · 08:00", false)], Instancias(documento, Firulais));
    }

    [Fact]
    public async Task Index_FallaDeBase_Devuelve500Generico()
    {
        using var conFalla = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.ConfigureServices(s => s.ConfigureDbContext<PetCareDbContext>(
                o => o.AddInterceptors(new FallaAlLeer())));
        });
        using var cliente = conFalla.CreateClient();

        using var respuesta = await cliente.GetAsync("/");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Contains("Ocurrió un error inesperado.", new HtmlParser().ParseDocument(cuerpo).Body?.TextContent ?? "");
        Assert.DoesNotContain(FallaAlLeer.Detalle, cuerpo);
        Assert.DoesNotContain("InvalidOperationException", cuerpo);
        Assert.DoesNotContain("Firulais", cuerpo);
    }

    [Fact]
    public async Task Estado_FallaDeBase_500Generico()
    {
        using var conFalla = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Production");
            b.ConfigureServices(s => s.ConfigureDbContext<PetCareDbContext>(
                o => o.AddInterceptors(new FallaAlLeer())));
        });
        using var cliente = conFalla.CreateClient();
        using var pedido = new HttpRequestMessage(HttpMethod.Get, "/?handler=Estado");
        pedido.Headers.Add("HX-Request", "true");

        using var respuesta = await cliente.SendAsync(pedido);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Contains("Ocurrió un error inesperado.", new HtmlParser().ParseDocument(cuerpo).Body?.TextContent ?? "");
        Assert.DoesNotContain(FallaAlLeer.Detalle, cuerpo);
        Assert.DoesNotContain("InvalidOperationException", cuerpo);
        Assert.DoesNotContain("Firulais", cuerpo);
    }

    [Fact]
    public async Task Index_ContenedorConPollingPausableYSync()
    {
        const string Trigger = "every 1s [!document.querySelector('[data-formulario-cumplimiento]')]";
        var pagina = await GetIndex();
        using var cliente = factory.CreateClient();
        using var respuesta = await cliente.GetAsync("/?handler=Estado");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var parcial = new HtmlParser().ParseDocument(await respuesta.Content.ReadAsStringAsync());

        // El contenedor que se reemplaza en cada refresco trae el mismo polling: si no, se cortaría al primero.
        foreach (var documento in new[] { pagina, parcial })
        {
            var contenedor = Assert.Single(documento.QuerySelectorAll("#estado-del-dia"));
            Assert.Equal("/?handler=Estado", contenedor.GetAttribute("hx-get"));
            Assert.Equal(Trigger, contenedor.GetAttribute("hx-trigger"));
            Assert.Equal("outerHTML", contenedor.GetAttribute("hx-swap"));
            Assert.Equal("this:replace", contenedor.GetAttribute("hx-sync"));
        }
    }

    [Fact]
    public async Task Index_NombreConHtml_SeMuestraCodificado()
    {
        int id;
        await using (var contexto = factory.CrearContexto())
        {
            var mascota = new Mascota { HogarId = 1, Nombre = "<b>x</b>" };
            contexto.Mascotas.Add(mascota);
            await contexto.SaveChangesAsync();
            id = mascota.Id;
        }

        using var cliente = factory.CreateClient();
        using var respuesta = await cliente.GetAsync("/");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        var documento = new HtmlParser().ParseDocument(cuerpo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.DoesNotContain("<b>x</b>", cuerpo);
        var seccion = Seccion(documento, id);
        Assert.Equal("<b>x</b>", Texto(seccion, "nombre-mascota"));
        Assert.Empty(seccion.QuerySelectorAll("b"));
    }

    [Fact]
    public async Task Index_ConRegistroDeHoy_MuestraRegistroConEliminar()
    {
        await Alta(Firulais, "paseo", "18:00", "solo-hoy");
        var instanciaId = await IdDeInstancia(Hoy);
        IrALocal(Hoy, 9, 10);
        int registroId;
        using (var scope = factory.Services.CreateScope())
        {
            var resultado = await scope.ServiceProvider.GetRequiredService<RegistrosService>()
                .RegistrarAsync(instanciaId, "09:05", CancellationToken.None);
            Assert.True(resultado.EsOk);
            registroId = resultado.Valor;
        }

        var documento = await GetIndex();

        var registro = Assert.Single(Seccion(documento, Firulais).QuerySelectorAll("[data-testid=registro]"));
        var id = registroId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(id, registro.GetAttribute("data-registro-id"));
        Assert.Equal("Paseo · 09:05", Texto(registro, "texto-registro"));
        var formulario = Assert.Single(registro.QuerySelectorAll("form"));
        Assert.Equal("post", formulario.GetAttribute("method"), ignoreCase: true);
        Assert.Equal("/?handler=EliminarRegistro", formulario.GetAttribute("action"));
        Assert.Equal("/?handler=EliminarRegistro", formulario.GetAttribute("hx-post"));
        Assert.Equal("#estado-del-dia", formulario.GetAttribute("hx-target"));
        Assert.Equal("outerHTML", formulario.GetAttribute("hx-swap"));
        Assert.Equal("closest #estado-del-dia:replace", formulario.GetAttribute("hx-sync"));
        Assert.Equal(id, formulario.QuerySelector("input[type=hidden][name=registroId]")?.GetAttribute("value"));
        Assert.Single(formulario.QuerySelectorAll("input[type=hidden][name=__RequestVerificationToken]"));
        var eliminar = Assert.Single(formulario.QuerySelectorAll("button[type=submit][data-testid=eliminar-registro]"));
        Assert.Equal("Eliminar", eliminar.TextContent.Trim());
        Assert.Empty(Seccion(documento, Michi).QuerySelectorAll("[data-testid=registro]"));
    }

    [Fact]
    public async Task Index_ConRegistrosDeHoyYAyer_MuestraResumen()
    {
        var ayer = Hoy.AddDays(-1);
        var anteayer = Hoy.AddDays(-2);
        // Una instancia por registro (índice único); la comida de ayer a las 22:00 es más tardía en
        // la hora pero anterior en el día que la de hoy a las 13:00.
        RegistroDirecto(TipoDeber.Comida, anteayer, new TimeOnly(23, 0));
        RegistroDirecto(TipoDeber.Comida, ayer, new TimeOnly(22, 0));
        RegistroDirecto(TipoDeber.Comida, Hoy, new TimeOnly(13, 0));
        RegistroDirecto(TipoDeber.Paseo, anteayer, new TimeOnly(23, 30));
        RegistroDirecto(TipoDeber.Paseo, ayer, new TimeOnly(8, 0));
        RegistroDirecto(TipoDeber.Paseo, ayer, new TimeOnly(21, 0));
        IrALocal(Hoy, 14, 0);

        var seccion = Seccion(await GetIndex(), Firulais);

        Assert.Equal("Última comida: 25/10/2026 13:00", Texto(seccion, "ultima-comida"));
        Assert.Equal("Último paseo: 24/10/2026 21:00", Texto(seccion, "ultimo-paseo"));
        Assert.Equal("0 paseos", Texto(seccion, "paseos-hoy"));
        Assert.Equal(
            ["Comida · 13:00"],
            seccion.QuerySelectorAll("[data-testid=registro] [data-testid=texto-registro]").Select(e => e.TextContent.Trim()));
    }

    private void RegistroDirecto(TipoDeber tipo, DateOnly fecha, TimeOnly hora)
    {
        var deber = factory.Base.InsertarDeber(Firulais, tipo, hora, Repeticion.SoloHoy, fecha);
        var instancia = factory.Base.InsertarInstancia(deber.Id, fecha, hora);
        factory.Base.InsertarRegistro(instancia.Id, fecha, hora);
    }

    /// <summary>Mueve el reloj a una hora local del hogar (UTC−3).</summary>
    private void IrALocal(DateOnly dia, int hora, int minuto) =>
        factory.Reloj.SetUtcNow(new DateTimeOffset(dia.ToDateTime(new TimeOnly(hora, minuto)), ZonaHogar.Desfase).ToUniversalTime());

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
        var instancias = scope.ServiceProvider.GetRequiredService<InstanciasService>();
        var resultado = await instancias.GenerarDelDiaAsync(CancellationToken.None);
        Assert.True(resultado.EsOk);
    }

    private async Task<int> IdDeInstancia(DateOnly fecha)
    {
        await using var contexto = factory.CrearContexto();
        return await contexto.Instancias.Where(i => i.Fecha == fecha).Select(i => i.Id).SingleAsync();
    }

    private async Task<IDocument> GetIndex()
    {
        using var cliente = factory.CreateClient();
        using var respuesta = await cliente.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return new HtmlParser().ParseDocument(await respuesta.Content.ReadAsStringAsync());
    }

    private static IElement Seccion(IDocument documento, int mascotaId) =>
        Assert.Single(documento.QuerySelectorAll($"section[data-testid=seccion-{mascotaId}]"));

    private static List<(string Texto, bool Pendiente)> Instancias(IDocument documento, int mascotaId) =>
        Seccion(documento, mascotaId).QuerySelectorAll("[data-testid=instancia]")
            .Select(i => (
                Texto(i, "texto-instancia") ?? "",
                i.QuerySelector("[data-testid=marca-pendiente]") is not null))
            .ToList();

    private static List<int> IdsDeInstancias(IDocument documento, int mascotaId) =>
        Seccion(documento, mascotaId).QuerySelectorAll("[data-testid=instancia]")
            .Select(i => int.Parse(i.GetAttribute("data-instancia-id") ?? "", System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

    private static string? Texto(IElement elemento, string testId) =>
        elemento.QuerySelector($"[data-testid={testId}]")?.TextContent.Trim();

    /// <summary>Simula una falla de la base al consultar (por ejemplo, base bloqueada o corrupta).</summary>
    private sealed class FallaAlLeer : DbCommandInterceptor
    {
        public const string Detalle = "detalle-secreto-de-la-falla-al-leer";

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result) =>
            throw new InvalidOperationException(Detalle);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(Detalle);
    }
}
