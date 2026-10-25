using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Instancias.Domain;

namespace PetCare.Features.Instancias.Data;

public class InstanciaConfiguration : IEntityTypeConfiguration<Instancia>
{
    public void Configure(EntityTypeBuilder<Instancia> builder)
    {
        builder.ToTable("Instancias");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Fecha).IsRequired();
        builder.Property(i => i.Hora).IsRequired();
        builder.HasIndex(i => new { i.DeberId, i.Fecha }).IsUnique();
        builder.HasIndex(i => i.Fecha);
        builder.HasOne<Deber>()
            .WithMany()
            .HasForeignKey(i => i.DeberId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
