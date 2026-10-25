using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Hogar.Domain;

namespace PetCare.Features.Deberes.Data;

public class DeberConfiguration : IEntityTypeConfiguration<Deber>
{
    public void Configure(EntityTypeBuilder<Deber> builder)
    {
        builder.ToTable("Deberes", t =>
        {
            t.HasCheckConstraint("CK_Deberes_Tipo", "\"Tipo\" IN (1, 2)");
            t.HasCheckConstraint("CK_Deberes_Repeticion", "\"Repeticion\" IN (1, 2)");
        });
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Tipo).IsRequired();
        builder.Property(d => d.Hora).IsRequired();
        builder.Property(d => d.Repeticion).IsRequired();
        builder.Property(d => d.FechaAlta).IsRequired();
        builder.HasIndex(d => d.MascotaId);
        builder.HasOne<Mascota>()
            .WithMany()
            .HasForeignKey(d => d.MascotaId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
