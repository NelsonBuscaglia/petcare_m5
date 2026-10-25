using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PetCare.Features.Hogar.Data;

// Dentro del namespace: si no, "Hogar" se resolvería al namespace PetCare.Features.Hogar.
using PetCare.Features.Hogar.Domain;

public class CuidadorConfiguration : IEntityTypeConfiguration<Cuidador>
{
    public void Configure(EntityTypeBuilder<Cuidador> builder)
    {
        builder.ToTable("Cuidadores");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Email).HasMaxLength(254).IsRequired().UseCollation("NOCASE");
        builder.HasIndex(c => c.Email).IsUnique();
        builder.HasOne<Hogar>()
            .WithMany()
            .HasForeignKey(c => c.HogarId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Cuidador { Id = 1, HogarId = 1, Email = "ana@ejemplo.com" },
            new Cuidador { Id = 2, HogarId = 1, Email = "beto@ejemplo.com" });
    }
}
