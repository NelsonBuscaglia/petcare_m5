using Microsoft.EntityFrameworkCore;
using PetCare.Features.Hogar.Domain;
using PetCare.Shared.Data;

namespace PetCare.Features.Hogar.Data;

public sealed class MascotasService(PetCareDbContext db)
{
    public Task<MascotaVista?> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        db.Mascotas
            .Where(m => m.Id == id)
            .Select(m => new MascotaVista(m.Id, m.Nombre))
            .SingleOrDefaultAsync(cancellationToken);
}
