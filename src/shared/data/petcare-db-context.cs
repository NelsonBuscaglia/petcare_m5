using Microsoft.EntityFrameworkCore;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Hogar.Domain;
using PetCare.Features.Instancias.Domain;

namespace PetCare.Shared.Data;

public class PetCareDbContext(DbContextOptions<PetCareDbContext> options) : DbContext(options)
{
    public DbSet<Hogar> Hogares => Set<Hogar>();

    public DbSet<Cuidador> Cuidadores => Set<Cuidador>();

    public DbSet<Mascota> Mascotas => Set<Mascota>();

    public DbSet<Deber> Deberes => Set<Deber>();

    public DbSet<Instancia> Instancias => Set<Instancia>();

    public DbSet<RegistroCumplimiento> RegistrosCumplimiento => Set<RegistroCumplimiento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PetCareDbContext).Assembly);
}
