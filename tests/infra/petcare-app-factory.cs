using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using PetCare.Shared.Data;

namespace PetCare.Tests.Infra;

/// <summary>
/// Host de la app para tests HTTP: SQLite en memoria (abierta mientras viva la fábrica, migrada
/// salvo que se pida lo contrario) y un <see cref="FakeTimeProvider"/> en lugar del reloj real.
/// Nunca abre <c>petcare.db</c>.
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
    {
        baseDeTest = BaseDeTest.Crear(migrar);
    }

    public FakeTimeProvider Reloj { get; } = new(InstanteInicial);

    public BaseDeTest Base => baseDeTest;

    public PetCareDbContext CrearContexto() => baseDeTest.CrearContexto();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PetCareDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<PetCareDbContext>>();
            services.AddDbContext<PetCareDbContext>(o => o.UseSqlite(baseDeTest.Conexion));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Reloj);
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
