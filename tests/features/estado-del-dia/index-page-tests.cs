using System.Data.Common;
using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PetCare.Features.Deberes.Data;
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
