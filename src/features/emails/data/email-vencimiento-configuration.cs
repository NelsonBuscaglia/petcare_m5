using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetCare.Features.Emails.Domain;
using PetCare.Features.Instancias.Domain;

namespace PetCare.Features.Emails.Data;

public class EmailVencimientoConfiguration : IEntityTypeConfiguration<EmailVencimiento>
{
    public void Configure(EntityTypeBuilder<EmailVencimiento> builder)
    {
        builder.ToTable("EmailsVencimiento");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AceptadoEnUtc).IsRequired();

        // Un solo email aceptado por instancia (FR-17): el índice resuelve también la carrera.
        builder.HasIndex(e => e.InstanciaId).IsUnique();
        builder.HasOne<Instancia>()
            .WithMany()
            .HasForeignKey(e => e.InstanciaId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
