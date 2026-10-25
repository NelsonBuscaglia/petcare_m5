using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace PetCare.Tests.Infra;

/// <summary>
/// App real sobre Kestrel (solo loopback, puerto libre elegido por el sistema) con la base SQLite
/// en memoria y el reloj falso de <see cref="PetCareAppFactory"/>, más un Chromium headless para
/// medir el layout en un navegador de verdad (AC-23).
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? playwright;
    private IBrowser? navegador;
    private Uri? urlBase;

    public PetCareAppFactory App { get; } = new();

    public IBrowser Navegador => navegador ?? throw new InvalidOperationException("La fixture no se inicializó.");

    public Uri UrlBase => urlBase ?? throw new InvalidOperationException("La fixture no se inicializó.");

    public async Task InitializeAsync()
    {
        App.UseKestrel(o => o.Listen(IPAddress.Loopback, 0));
        App.StartServer();
        var direcciones = App.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses
            ?? throw new InvalidOperationException("Kestrel no informó sus direcciones.");
        urlBase = new Uri(direcciones.Single());

        // Idempotente: si Chromium ya está descargado en el caché de Playwright, no hace nada.
        var codigo = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (codigo != 0)
        {
            throw new InvalidOperationException($"No se pudo instalar Chromium para Playwright (código {codigo}).");
        }

        playwright = await Playwright.CreateAsync();
        navegador = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (navegador is not null)
        {
            await navegador.DisposeAsync();
        }

        playwright?.Dispose();
        await App.DisposeAsync();
    }
}
