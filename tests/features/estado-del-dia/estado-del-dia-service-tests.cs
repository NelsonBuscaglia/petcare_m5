using Microsoft.Extensions.Time.Testing;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.EstadoDelDia.Data;
using PetCare.Shared.Tiempo;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.EstadoDelDia;

// Dentro del namespace: si no, "EstadoDelDia" se resolvería al namespace PetCare.Tests.Features.EstadoDelDia.
using PetCare.Features.EstadoDelDia.Domain;

public sealed class EstadoDelDiaServiceTests : IDisposable
{
    private const int Firulais = 1;
    private const int Michi = 2;

    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Ayer = Hoy.AddDays(-1);
    private static readonly DateOnly Anteayer = Hoy.AddDays(-2);

    private readonly BaseDeTest baseDeTest = BaseDeTest.Crear();
    private readonly FakeTimeProvider reloj = new(PetCareAppFactory.InstanteInicial);

    public void Dispose() => baseDeTest.Dispose();

    [Fact]
    public async Task EstadoDelDiaService_ConsultaAcotada_TraeHoyYUltimos()
    {
        // Firulais: comida de anteayer y de ayer (la última, de ayer); paseos de los 3 días (2 hoy).
        Registrar(Firulais, TipoDeber.Comida, Anteayer, 8, 0);
        var ultimaComidaFirulais = Registrar(Firulais, TipoDeber.Comida, Ayer, 20, 0);
        Registrar(Firulais, TipoDeber.Paseo, Anteayer, 18, 0);
        Registrar(Firulais, TipoDeber.Paseo, Ayer, 7, 0);
        Registrar(Firulais, TipoDeber.Paseo, Ayer, 21, 0);
        var paseoFirulais0905 = Registrar(Firulais, TipoDeber.Paseo, Hoy, 9, 5);
        var paseoFirulais0800 = Registrar(Firulais, TipoDeber.Paseo, Hoy, 8, 0);
        // Michi: la comida de hoy es también la última; el último paseo es de ayer a la misma hora que el de anteayer.
        Registrar(Michi, TipoDeber.Comida, Anteayer, 12, 0);
        var comidaMichiHoy = Registrar(Michi, TipoDeber.Comida, Hoy, 8, 30);
        Registrar(Michi, TipoDeber.Paseo, Anteayer, 10, 0);
        var ultimoPaseoMichi = Registrar(Michi, TipoDeber.Paseo, Ayer, 10, 0);
        reloj.SetUtcNow(ZonaHogar.AUtc(Hoy.ToDateTime(new TimeOnly(10, 0))));

        await using var contexto = baseDeTest.CrearContexto();
        var servicio = new EstadoDelDiaService(contexto, reloj);
        var leidos = await servicio.LeerRegistrosAsync(Hoy, CancellationToken.None);

        Assert.Equal(
            new[] { ultimaComidaFirulais, paseoFirulais0905, paseoFirulais0800, comidaMichiHoy, ultimoPaseoMichi }.Order(),
            leidos.Select(r => r.Id).Distinct().Order());
        var paseo0905 = Assert.Single(leidos.DistinctBy(r => r.Id), r => r.Id == paseoFirulais0905);
        Assert.Equal(new RegistroLeido(paseoFirulais0905, Firulais, TipoDeber.Paseo, Hoy, new TimeOnly(9, 5)), paseo0905);

        // La unión acotada arma el mismo resumen que la lectura completa.
        var estado = await servicio.ObtenerAsync(CancellationToken.None);
        var firulais = Assert.Single(estado.Secciones, s => s.MascotaId == Firulais);
        Assert.Equal(2, firulais.PaseosHoy);
        Assert.Equal(new UltimoRegistro(Ayer, new TimeOnly(20, 0)), firulais.UltimaComida);
        Assert.Equal(new UltimoRegistro(Hoy, new TimeOnly(9, 5)), firulais.UltimoPaseo);
        Assert.Equal([paseoFirulais0800, paseoFirulais0905], firulais.RegistrosHoy.Select(r => r.Id));
        var michi = Assert.Single(estado.Secciones, s => s.MascotaId == Michi);
        Assert.Equal(0, michi.PaseosHoy);
        Assert.Equal(new UltimoRegistro(Hoy, new TimeOnly(8, 30)), michi.UltimaComida);
        Assert.Equal(new UltimoRegistro(Ayer, new TimeOnly(10, 0)), michi.UltimoPaseo);
        Assert.Equal([comidaMichiHoy], michi.RegistrosHoy.Select(r => r.Id));
    }

    /// <summary>Un deber, su instancia de ese día y el registro de cumplimiento; devuelve el id del registro.</summary>
    private int Registrar(int mascotaId, TipoDeber tipo, DateOnly fecha, int hora, int minuto)
    {
        var horaReal = new TimeOnly(hora, minuto);
        var deber = baseDeTest.InsertarDeber(mascotaId, tipo, horaReal, Repeticion.SoloHoy, fecha);
        var instancia = baseDeTest.InsertarInstancia(deber.Id, fecha, horaReal);
        return baseDeTest.InsertarRegistro(instancia.Id, fecha, horaReal).Id;
    }
}
