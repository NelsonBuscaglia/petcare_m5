using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using PetCare.Features.Deberes.Data;
using PetCare.Features.Hogar.Domain;
using PetCare.Features.Instancias.Data;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.EstadoDelDia;

/// <summary>
/// AC-23 / NFR-02 de FEAT-001a y AC-30 / NFR-04 de FEAT-001b: las pantallas, el formulario de
/// cumplimiento y la lista de registros, a 360 px, sin scroll horizontal. Cada test usa una mascota
/// propia con un nombre de 50 caracteres sin espacios (el máximo del esquema): es el texto que más
/// empuja el ancho.
/// </summary>
[Trait("Categoria", "Navegador")]
public sealed class SinScrollHorizontalTests(PlaywrightFixture fixture) : IClassFixture<PlaywrightFixture>
{
    private const int Ancho = 360;
    private const int Alto = 740;

    [Fact]
    public async Task SinScrollHorizontal_360px_EstadoDelDia()
    {
        var mascotaId = await CrearMascotaDeNombreLargo("estado");
        await Alta(mascotaId, "comida", "08:00", "todos-los-dias");
        await Alta(mascotaId, "paseo", "18:00", "todos-los-dias");
        await using var contexto = await AbrirContexto();
        var pagina = await contexto.NewPageAsync();

        var respuesta = await pagina.GotoAsync("/");

        Assert.Equal(200, respuesta?.Status);
        Assert.Equal(2, await pagina.Locator($"section[data-testid=seccion-{mascotaId}] [data-testid=instancia]").CountAsync());
        await AssertSinScrollHorizontal(pagina);
    }

    [Fact]
    public async Task SinScrollHorizontal_360px_ConfiguracionDeDeberes()
    {
        var mascotaId = await CrearMascotaDeNombreLargo("configuracion");
        await Alta(mascotaId, "comida", "08:00", "todos-los-dias");
        await Alta(mascotaId, "paseo", "18:00", "todos-los-dias");
        await Alta(mascotaId, "comida", "20:00", "solo-hoy");
        await using var contexto = await AbrirContexto();
        var pagina = await contexto.NewPageAsync();

        var respuesta = await pagina.GotoAsync($"/mascotas/{mascotaId}/deberes");

        Assert.Equal(200, respuesta?.Status);
        Assert.Equal(3, await pagina.Locator("[data-testid=deber-activo]").CountAsync());
        await AssertSinScrollHorizontal(pagina);
    }

    [Fact]
    public async Task SinScrollHorizontal_360px_AltaDeDeberConErrores()
    {
        var mascotaId = await CrearMascotaDeNombreLargo("alta");
        await using var contexto = await AbrirContexto();
        var pagina = await contexto.NewPageAsync();
        await pagina.GotoAsync($"/mascotas/{mascotaId}/deberes");
        await AssertSinScrollHorizontal(pagina);
        // Si el formulario hiciera un POST normal, la página se recargaría y esta marca desaparecería.
        await pagina.EvaluateAsync("() => { window.__sinRecarga = true; }");

        await pagina.GetByRole(AriaRole.Button, new() { Name = "Dar de alta" }).ClickAsync();
        await pagina.Locator("[data-testid=error]").Nth(2).WaitForAsync();

        Assert.Equal(3, await pagina.Locator("[data-testid=error]").CountAsync());
        await AssertSinScrollHorizontal(pagina);

        await pagina.CheckAsync("input[name=tipo][value=paseo]");
        await pagina.FillAsync("input[name=hora]", "18:00");
        await pagina.CheckAsync("input[name=repeticion][value=todos-los-dias]");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Dar de alta" }).ClickAsync();
        await pagina.Locator("[data-testid=deber-activo]").First.WaitForAsync();

        Assert.Equal(
            ["Paseo · 18:00 · todos los días"],
            (await pagina.Locator("[data-testid=deber-activo]").AllTextContentsAsync()).Select(t => t.Trim()));
        Assert.Equal(0, await pagina.Locator("[data-testid=error]").CountAsync());
        Assert.True(await pagina.EvaluateAsync<bool>("() => window.__sinRecarga === true"), "El alta recargó la página: HTMX no intervino.");
        await AssertSinScrollHorizontal(pagina);
    }

    [Fact]
    public async Task SinScrollHorizontal_360px_RegistrarCumplimientoEditandoHora()
    {
        // El reloj de esta fixture no se mueve: son las 09:00 del 25/10 y la comida de las 08:00 está vigente.
        var mascotaId = await CrearMascotaDeNombreLargo("registrar");
        await Alta(mascotaId, "comida", "08:00", "solo-hoy");
        await using var contexto = await AbrirContexto();
        var pagina = await contexto.NewPageAsync();
        await pagina.GotoAsync("/");
        var seccion = pagina.Locator($"section[data-testid=seccion-{mascotaId}]");
        await AssertSinScrollHorizontal(pagina);

        await seccion.Locator("[data-testid=cumplido]").ClickAsync();
        await seccion.Locator("form[data-formulario-cumplimiento]").WaitForAsync();
        await AssertSinScrollHorizontal(pagina);

        await seccion.Locator("[data-testid=hora-real]").FillAsync("23:59");
        await seccion.Locator("[data-testid=confirmar]").ClickAsync();
        await seccion.Locator("[data-testid=error-cumplimiento]").WaitForAsync();
        await AssertSinScrollHorizontal(pagina);

        await seccion.Locator("[data-testid=hora-real]").FillAsync("08:30");
        await seccion.Locator("[data-testid=confirmar]").ClickAsync();
        await seccion.Locator("[data-testid=registro]").WaitForAsync();

        Assert.Equal(0, await seccion.Locator("[data-testid=instancia]").CountAsync());
        Assert.Equal("Comida · 08:30", (await seccion.Locator("[data-testid=texto-registro]").TextContentAsync())?.Trim());
        await AssertSinScrollHorizontal(pagina);
    }

    [Fact]
    public async Task SinScrollHorizontal_360px_EliminarRegistro()
    {
        var mascotaId = await CrearMascotaDeNombreLargo("eliminar");
        await Alta(mascotaId, "paseo", "08:00", "solo-hoy");
        await Alta(mascotaId, "comida", "08:15", "solo-hoy");
        await Registrar(mascotaId, "08:30");
        await using var contexto = await AbrirContexto();
        var pagina = await contexto.NewPageAsync();
        await pagina.GotoAsync("/");
        var seccion = pagina.Locator($"section[data-testid=seccion-{mascotaId}]");

        Assert.Equal(2, await seccion.Locator("[data-testid=registro]").CountAsync());
        await AssertSinScrollHorizontal(pagina);

        await seccion.Locator("[data-testid=eliminar-registro]").First.ClickAsync();
        await seccion.Locator("[data-testid=instancia]").WaitForAsync();

        Assert.Equal(1, await seccion.Locator("[data-testid=registro]").CountAsync());
        await AssertSinScrollHorizontal(pagina);
    }

    private Task<IBrowserContext> AbrirContexto() => fixture.AbrirContextoAsync(Ancho, Alto);

    private static async Task AssertSinScrollHorizontal(IPage pagina)
    {
        var medidas = await pagina.EvaluateAsync<int[]>(
            "() => [document.documentElement.scrollWidth, window.innerWidth]");
        var (scrollWidth, innerWidth) = (medidas[0], medidas[1]);

        // En viewport móvil, innerWidth es 360 solo si la página declara <meta name="viewport" ...> y
        // nada la desborda: si el contenido es más ancho, Chromium agranda el viewport para que entre.
        Assert.Equal(Ancho, innerWidth);
        Assert.True(scrollWidth <= innerWidth, $"Hay scroll horizontal: scrollWidth {scrollWidth} > innerWidth {innerWidth}.");
    }

    private async Task<int> CrearMascotaDeNombreLargo(string sufijo)
    {
        var nombre = ("Mascotaconunnombremuylargoysinespacios" + sufijo).PadRight(50, 'x')[..50];
        await using var contexto = fixture.App.CrearContexto();
        var mascota = new Mascota { HogarId = 1, Nombre = nombre };
        contexto.Mascotas.Add(mascota);
        await contexto.SaveChangesAsync();
        return mascota.Id;
    }

    /// <summary>Registra el cumplimiento de todas las instancias de hoy de la mascota a la hora dada.</summary>
    private async Task Registrar(int mascotaId, string hora)
    {
        List<int> instancias;
        await using (var contexto = fixture.App.CrearContexto())
        {
            instancias = contexto.Instancias
                .Where(i => contexto.Deberes.Any(d => d.Id == i.DeberId && d.MascotaId == mascotaId))
                .Select(i => i.Id)
                .ToList();
        }

        using var scope = fixture.App.Services.CreateScope();
        var registros = scope.ServiceProvider.GetRequiredService<RegistrosService>();
        foreach (var instanciaId in instancias)
        {
            var resultado = await registros.RegistrarAsync(instanciaId, hora, CancellationToken.None);
            Assert.True(resultado.EsOk, $"El registro de la instancia {instanciaId} fue rechazado.");
        }
    }

    private async Task Alta(int mascotaId, string tipo, string hora, string repeticion)
    {
        using var scope = fixture.App.Services.CreateScope();
        var deberes = scope.ServiceProvider.GetRequiredService<DeberesService>();
        var resultado = await deberes.DarDeAltaAsync(mascotaId, tipo, hora, repeticion, CancellationToken.None);
        Assert.True(resultado.EsOk, $"El alta de {tipo} {hora} {repeticion} fue rechazada.");
    }
}
