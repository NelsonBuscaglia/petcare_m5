using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using PetCare.Features.Emails.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Hosting;

namespace PetCare.Tests.Infra;

/// <summary>
/// Host de la app para tests HTTP: SQLite en memoria (abierta mientras viva la fábrica, migrada
/// salvo que se pida lo contrario) y un <see cref="FakeTimeProvider"/> en lugar del reloj real.
/// Sin <see cref="GeneracionDiariaWorker"/> ni <see cref="VencimientosWorker"/>: los tests generan
/// instancias y procesan vencimientos de forma explícita, y los de los workers los instancian aparte.
/// El envío de emails es un <see cref="EnvioEmailFalso"/> (nada se escribe en la consola ni sale por
/// SMTP). El estado del día se refresca cada 1 s (en vez de 10) para que los tests de navegador no
/// esperen de más. Nunca abre <c>petcare.db</c>.
/// </summary>
public class PetCareAppFactory : WebApplicationFactory<Program>
{
    /// <summary>25/10/2026 12:00 UTC (09:00 en UTC−3).</summary>
    public static readonly DateTimeOffset InstanteInicial = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    private readonly BaseDeTest baseDeTest;

    public PetCareAppFactory()
        : this(migrar: true)
    {
    }

    public PetCareAppFactory(bool migrar)
        : this(migrar, new FakeTimeProvider(InstanteInicial))
    {
    }

    private PetCareAppFactory(bool migrar, FakeTimeProvider reloj)
    {
        baseDeTest = BaseDeTest.Crear(migrar);
        Reloj = reloj;
        EnvioEmail = new EnvioEmailFalso(reloj);
    }

    public FakeTimeProvider Reloj { get; }

    public EnvioEmailFalso EnvioEmail { get; }

    public BaseDeTest Base => baseDeTest;

    public PetCareDbContext CrearContexto() => baseDeTest.CrearContexto();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("EstadoDelDia:RefrescoSegundos", "1");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PetCareDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<PetCareDbContext>>();
            services.AddDbContext<PetCareDbContext>(o => o.UseSqlite(baseDeTest.Conexion));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Reloj);

            services.Remove(services.Single(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(GeneracionDiariaWorker)));
            services.Remove(services.Single(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(VencimientosWorker)));

            services.RemoveAll<IEnvioEmail>();
            services.AddSingleton<IEnvioEmail>(EnvioEmail);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            baseDeTest.Dispose();
        }
    }
}
