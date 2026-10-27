using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using PetCare.Features.Deberes.Data;
using PetCare.Features.Hogar.Domain;
using PetCare.Features.Instancias.Data;
using PetCare.Shared.Tiempo;
using PetCare.Tests.Infra;
using static Microsoft.Playwright.Assertions;

namespace PetCare.Tests.Features.EstadoDelDia;

/// <summary>
/// FR-01 / NFR-02: el estado del día se actualiza solo, sin recargar, en ≤ 15 s; el refresco se pausa
/// con el formulario de cumplimiento abierto. La fábrica refresca cada 1 s. Cada test usa una mascota
/// propia y mueve el reloj (que nunca retrocede) a la próxima hora local que necesita.
/// </summary>
[Trait("Categoria", "Navegador")]
public sealed class RefrescoNavegadorTests(PlaywrightFixture fixture) : IClassFixture<PlaywrightFixture>
{
    /// <summary>NFR-02: lo que puede tardar un cambio en verse sin recargar.</summary>
    private const float LimiteMs = 15_000;

    private static readonly LocatorAssertionsToHaveCountOptions DentroDelLimite = new() { Timeout = LimiteMs };

    [Fact]
    public async Task Refresco_OtroCuidadorRegistra_DesapareceYSumaPaseo()
    {
        IrAProxima(new TimeOnly(10, 0));
        var mascotaId = await CrearMascota("Registra");
        await Alta(mascotaId, "paseo", "09:00", "solo-hoy");
        await using var contextoA = await fixture.AbrirContextoAsync();
        await using var contextoB = await fixture.AbrirContextoAsync();
        var paginaA = await AbrirEstado(contextoA);
        var paginaB = await AbrirEstado(contextoB);
        var seccionA = Seccion(paginaA, mascotaId);
        var seccionB = Seccion(paginaB, mascotaId);
        await Expect(seccionA.Locator("[data-testid=instancia]")).ToHaveCountAsync(1);
        await Expect(seccionA.Locator("[data-testid=paseos-hoy]")).ToHaveTextAsync("0 paseos");

        await seccionB.Locator("[data-testid=cumplido]").ClickAsync();
        await seccionB.Locator("[data-testid=confirmar]").ClickAsync();
        await Expect(seccionB.Locator("[data-testid=instancia]")).ToHaveCountAsync(0);

        await Expect(seccionA.Locator("[data-testid=instancia]")).ToHaveCountAsync(0, DentroDelLimite);
        await Expect(seccionA.Locator("[data-testid=paseos-hoy]")).ToHaveTextAsync("1 paseo");
        await AssertSinRecarga(paginaA);
    }

    [Fact]
    public async Task Refresco_SinInteraccion_MarcaPendienteA1215()
    {
        var dia = IrAProxima(new TimeOnly(12, 14));
        var mascotaId = await CrearMascota("Pendiente");
        await Alta(mascotaId, "paseo", "12:00", "solo-hoy");
        await using var contexto = await fixture.AbrirContextoAsync();
        var pagina = await AbrirEstado(contexto);
        var seccion = Seccion(pagina, mascotaId);
        await Expect(seccion.Locator("[data-testid=instancia]")).ToHaveCountAsync(1);
        await Expect(seccion.Locator("[data-testid=marca-pendiente]")).ToHaveCountAsync(0);

        fixture.Reloj.SetUtcNow(ZonaHogar.AUtc(dia.ToDateTime(new TimeOnly(12, 15))));

        await Expect(seccion.Locator("[data-testid=marca-pendiente]")).ToHaveCountAsync(1, DentroDelLimite);
        await AssertSinRecarga(pagina);
    }

    [Fact]
    public async Task Refresco_OtroCuidadorDaDeAlta_ApareceInstancia()
    {
        IrAProxima(new TimeOnly(12, 0));
        var mascotaId = await CrearMascota("Alta");
        await using var contextoA = await fixture.AbrirContextoAsync();
        await using var contextoB = await fixture.AbrirContextoAsync();
        var paginaA = await AbrirEstado(contextoA);
        var seccionA = Seccion(paginaA, mascotaId);
        await Expect(seccionA.Locator("[data-testid=sin-tareas]")).ToHaveCountAsync(1);

        var paginaB = await contextoB.NewPageAsync();
        await paginaB.GotoAsync($"/mascotas/{mascotaId}/deberes");
        await paginaB.CheckAsync("input[name=tipo][value=comida]");
        await paginaB.FillAsync("input[name=hora]", "21:00");
        await paginaB.CheckAsync("input[name=repeticion][value=solo-hoy]");
        await paginaB.GetByRole(AriaRole.Button, new() { Name = "Dar de alta" }).ClickAsync();
        await Expect(paginaB.Locator("[data-testid=deber-activo]")).ToHaveCountAsync(1);

        await Expect(seccionA.Locator("[data-testid=instancia]")).ToHaveCountAsync(1, DentroDelLimite);
        await Expect(seccionA.Locator("[data-testid=texto-instancia]")).ToHaveTextAsync("Comida · 21:00");
        await Expect(seccionA.Locator("[data-testid=sin-tareas]")).ToHaveCountAsync(0);
        await AssertSinRecarga(paginaA);
    }

    [Fact]
    public async Task Refresco_Medianoche_DesapareceInstancia()
    {
        var dia = IrAProxima(new TimeOnly(23, 59));
        var mascotaId = await CrearMascota("Medianoche");
        await Alta(mascotaId, "paseo", "20:00", "todos-los-dias");
        await using var contexto = await fixture.AbrirContextoAsync();
        var pagina = await AbrirEstado(contexto);
        var seccion = Seccion(pagina, mascotaId);
        var instancias = seccion.Locator("[data-testid=instancia]");
        await Expect(instancias).ToHaveCountAsync(1);
        var idAyer = await instancias.First.GetAttributeAsync("data-instancia-id");
        Assert.False(string.IsNullOrEmpty(idAyer));

        fixture.Reloj.SetUtcNow(ZonaHogar.AUtc(dia.AddDays(1).ToDateTime(TimeOnly.MinValue)));

        // Con repetición diaria el día nuevo trae su propia instancia (la genera el worker, que en
        // los tests no corre): lo que tiene que dejar de verse es la de ayer, identificada por su id.
        await Expect(seccion.Locator($"[data-testid=instancia][data-instancia-id='{idAyer}']"))
            .ToHaveCountAsync(0, DentroDelLimite);
        await AssertSinRecarga(pagina);
    }

    [Fact]
    public async Task Refresco_FormularioAbierto_NoSeCierraPorPolling()
    {
        IrAProxima(new TimeOnly(10, 0));
        var mascotaId = await CrearMascota("Formulario");
        await Alta(mascotaId, "paseo", "09:00", "solo-hoy");
        await using var contexto = await fixture.AbrirContextoAsync();
        var pagina = await AbrirEstado(contexto);
        var refrescos = ContarRefrescos(pagina);
        var seccion = Seccion(pagina, mascotaId);
        await EsperarRefresco(pagina);

        await seccion.Locator("[data-testid=cumplido]").ClickAsync();
        await seccion.Locator("[data-testid=hora-real]").FillAsync("09:30");
        var refrescosConFormulario = refrescos();
        await pagina.WaitForTimeoutAsync(3_000);

        await Expect(seccion.Locator("form[data-formulario-cumplimiento]")).ToHaveCountAsync(1);
        await Expect(seccion.Locator("[data-testid=hora-real]")).ToHaveValueAsync("09:30");
        Assert.Equal(refrescosConFormulario, refrescos());
    }

    [Fact]
    public async Task Cancelar_CierraFormularioYReanudaRefresco()
    {
        IrAProxima(new TimeOnly(10, 0));
        var mascotaId = await CrearMascota("Cancelar");
        await Alta(mascotaId, "paseo", "09:00", "solo-hoy");
        await Alta(mascotaId, "comida", "09:30", "solo-hoy");
        await using var contexto = await fixture.AbrirContextoAsync();
        var pagina = await AbrirEstado(contexto);
        var seccion = Seccion(pagina, mascotaId);
        var instancias = seccion.Locator("[data-testid=instancia]");
        await Expect(instancias).ToHaveCountAsync(2);
        var idComida = int.Parse(
            await instancias.Nth(1).GetAttributeAsync("data-instancia-id") ?? "",
            System.Globalization.CultureInfo.InvariantCulture);

        await instancias.Nth(0).Locator("[data-testid=cumplido]").ClickAsync();
        await seccion.Locator("form[data-formulario-cumplimiento]").WaitForAsync();
        await seccion.Locator("[data-testid=cancelar]").ClickAsync();
        await Expect(seccion.Locator("form[data-formulario-cumplimiento]")).ToHaveCountAsync(0);
        await Expect(instancias).ToHaveCountAsync(2);

        // Otro cuidador registra la comida: el refresco, ya reanudado, la saca de la lista.
        using (var scope = fixture.App.Services.CreateScope())
        {
            var resultado = await scope.ServiceProvider.GetRequiredService<RegistrosService>()
                .RegistrarAsync(idComida, "09:45", CancellationToken.None);
            Assert.True(resultado.EsOk);
        }

        await Expect(instancias).ToHaveCountAsync(1, DentroDelLimite);
        await Expect(seccion.Locator("[data-testid=texto-instancia]")).ToHaveTextAsync("Paseo · 09:00");
        await AssertSinRecarga(pagina);
    }

    [Fact]
    public async Task RegistrarCumplimiento_HoraPorDefecto_DosClics()
    {
        IrAProxima(new TimeOnly(10, 0));
        var mascotaId = await CrearMascota("DosClics");
        await Alta(mascotaId, "paseo", "09:00", "solo-hoy");
        await using var contexto = await fixture.AbrirContextoAsync();
        var pagina = await AbrirEstado(contexto);
        var seccion = Seccion(pagina, mascotaId);
        // NFR-03 vale con el refresco automático andando: un refresco entre los dos toques no debe
        // cerrar el formulario.
        await EsperarRefresco(pagina);
        await pagina.EvaluateAsync("() => { window.__clics = 0; document.addEventListener('click', () => window.__clics++, true); }");

        await seccion.Locator("[data-testid=cumplido]").ClickAsync();
        await pagina.WaitForTimeoutAsync(1_500);
        await seccion.Locator("[data-testid=confirmar]").ClickAsync();

        await Expect(seccion.Locator("[data-testid=instancia]")).ToHaveCountAsync(0);
        await Expect(seccion.Locator("[data-testid=texto-registro]")).ToHaveTextAsync("Paseo · 10:00");
        await Expect(seccion.Locator("[data-testid=paseos-hoy]")).ToHaveTextAsync("1 paseo");
        Assert.Equal(2, await pagina.EvaluateAsync<int>("() => window.__clics"));
    }

    /// <summary>
    /// Mueve el reloj a la próxima vez que sea <paramref name="hora"/> en el hogar (hoy si todavía no
    /// pasó, si no mañana) y devuelve ese día. El reloj falso no puede retroceder.
    /// </summary>
    private DateOnly IrAProxima(TimeOnly hora)
    {
        var ahora = ZonaHogar.AhoraLocal(fixture.Reloj.GetUtcNow());
        var destino = DateOnly.FromDateTime(ahora).ToDateTime(hora);
        if (destino < ahora)
        {
            destino = destino.AddDays(1);
        }

        fixture.Reloj.SetUtcNow(ZonaHogar.AUtc(destino));
        return DateOnly.FromDateTime(destino);
    }

    private static async Task<IPage> AbrirEstado(IBrowserContext contexto)
    {
        var pagina = await contexto.NewPageAsync();
        var respuesta = await pagina.GotoAsync("/");
        Assert.Equal(200, respuesta?.Status);
        // Si la página se recargara, esta marca desaparecería.
        await pagina.EvaluateAsync("() => { window.__sinRecarga = true; }");
        return pagina;
    }

    private static async Task AssertSinRecarga(IPage pagina) =>
        Assert.True(await pagina.EvaluateAsync<bool>("() => window.__sinRecarga === true"), "La página se recargó.");

    /// <summary>Cuenta los pedidos de refresco (<c>GET /?handler=Estado</c>) que hace la página.</summary>
    private static Func<int> ContarRefrescos(IPage pagina)
    {
        var cantidad = 0;
        pagina.Request += (_, pedido) =>
        {
            if (EsRefresco(pedido))
            {
                Interlocked.Increment(ref cantidad);
            }
        };
        return () => Volatile.Read(ref cantidad);
    }

    /// <summary>Espera a que el refresco automático haga al menos un pedido: confirma que está andando.</summary>
    private static Task EsperarRefresco(IPage pagina) =>
        pagina.WaitForRequestAsync(EsRefresco, new() { Timeout = LimiteMs });

    private static bool EsRefresco(IRequest pedido) =>
        pedido.Method == "GET" && pedido.Url.Contains("handler=Estado", StringComparison.Ordinal);

    private static ILocator Seccion(IPage pagina, int mascotaId) =>
        pagina.Locator($"section[data-testid=seccion-{mascotaId}]");

    private async Task<int> CrearMascota(string nombre)
    {
        await using var contexto = fixture.App.CrearContexto();
        var mascota = new Mascota { HogarId = 1, Nombre = nombre };
        contexto.Mascotas.Add(mascota);
        await contexto.SaveChangesAsync();
        return mascota.Id;
    }

    private async Task Alta(int mascotaId, string tipo, string hora, string repeticion)
    {
        using var scope = fixture.App.Services.CreateScope();
        var deberes = scope.ServiceProvider.GetRequiredService<DeberesService>();
        var resultado = await deberes.DarDeAltaAsync(mascotaId, tipo, hora, repeticion, CancellationToken.None);
        Assert.True(resultado.EsOk, $"El alta de {tipo} {hora} {repeticion} fue rechazada.");
    }
}
