using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PetCare.Features.Hogar.Data;

// Dentro del namespace: si no, "Hogar" se resolvería al namespace PetCare.Features.Hogar.
using PetCare.Features.Hogar.Domain;

public class HogarConfiguration : IEntityTypeConfiguration<Hogar>
{
    public void Configure(EntityTypeBuilder<Hogar> builder)
    {
        builder.ToTable("Hogares");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Nombre).HasMaxLength(100).IsRequired();

        builder.HasData(new Hogar { Id = 1, Nombre = "Hogar" });
    }
}
