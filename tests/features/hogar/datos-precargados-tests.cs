using Microsoft.EntityFrameworkCore;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.Hogar;

public class DatosPrecargadosTests
{
    [Fact]
    public async Task Seed_CreaHogarCuidadoresYMascotas()
    {
        using var baseDeTest = BaseDeTest.Crear();
        await using var contexto = baseDeTest.CrearContexto();

        var hogares = await contexto.Hogares.ToListAsync();
        var emails = await contexto.Cuidadores.OrderBy(c => c.Email).Select(c => c.Email).ToListAsync();
        var mascotas = await contexto.Mascotas.OrderBy(m => m.Nombre).Select(m => m.Nombre).ToListAsync();
        var hogarDeMascotas = await contexto.Mascotas.Select(m => m.HogarId).Distinct().ToListAsync();
        var hogarDeCuidadores = await contexto.Cuidadores.Select(c => c.HogarId).Distinct().ToListAsync();

        var hogar = Assert.Single(hogares);
        Assert.Equal(["ana@ejemplo.com", "beto@ejemplo.com"], emails);
        Assert.Equal(["Firulais", "Michi"], mascotas);
        Assert.Equal([hogar.Id], hogarDeMascotas);
        Assert.Equal([hogar.Id], hogarDeCuidadores);
        Assert.Equal(0, await contexto.Deberes.CountAsync());
    }
}
