using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetCare.Features.Instancias.Domain;

namespace PetCare.Features.Instancias.Data;

public class RegistroCumplimientoConfiguration : IEntityTypeConfiguration<RegistroCumplimiento>
{
    public void Configure(EntityTypeBuilder<RegistroCumplimiento> builder)
    {
        builder.ToTable("RegistrosCumplimiento");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Fecha).IsRequired();
        builder.Property(r => r.HoraReal).IsRequired();
        builder.HasIndex(r => r.InstanciaId).IsUnique();
        builder.HasIndex(r => r.Fecha);
        builder.HasOne<Instancia>()
            .WithMany()
            .HasForeignKey(r => r.InstanciaId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
