using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PetCare.Features.Hogar.Data;

// Dentro del namespace: si no, "Hogar" se resolvería al namespace PetCare.Features.Hogar.
using PetCare.Features.Hogar.Domain;

public class MascotaConfiguration : IEntityTypeConfiguration<Mascota>
{
    public void Configure(EntityTypeBuilder<Mascota> builder)
    {
        builder.ToTable("Mascotas");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Nombre).HasMaxLength(50).IsRequired().UseCollation("NOCASE");
        builder.HasIndex(m => new { m.HogarId, m.Nombre }).IsUnique();
        builder.HasOne<Hogar>()
            .WithMany()
            .HasForeignKey(m => m.HogarId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Mascota { Id = 1, HogarId = 1, Nombre = "Firulais" },
            new Mascota { Id = 2, HogarId = 1, Nombre = "Michi" });
    }
}
