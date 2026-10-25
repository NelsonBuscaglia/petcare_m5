using PetCare.Features.Deberes.Domain;
using PetCare.Features.Hogar.Domain;

namespace PetCare.Tests.Features.EstadoDelDia;

// Dentro del namespace: si no, "EstadoDelDia" se resolvería al namespace PetCare.Tests.Features.EstadoDelDia.
using PetCare.Features.EstadoDelDia.Domain;

public class ArmadoEstadoDelDiaTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Ayer = Hoy.AddDays(-1);
    private static readonly MascotaVista Firulais = new(1, "Firulais");
    private static readonly MascotaVista Michi = new(2, "Michi");
    private static readonly IReadOnlyList<MascotaVista> Mascotas = [Firulais, Michi];

    [Fact]
    public void Armar_ComidaYPaseoA1200_ComidaPendientePaseoSinMarca()
    {
        IReadOnlyList<InstanciaLeida> instancias =
        [
            new(10, Firulais.Id, TipoDeber.Paseo, Hoy, new TimeOnly(18, 0), TieneRegistro: false),
            new(11, Firulais.Id, TipoDeber.Comida, Hoy, new TimeOnly(8, 0), TieneRegistro: false),
        ];

        var estado = ArmadoEstadoDelDia.Armar(Mascotas, instancias, [], A(Hoy, 12, 0));

        Assert.Equal([Firulais.Id, Michi.Id], estado.Secciones.Select(s => s.MascotaId));
        Assert.Equal(
            [
                new InstanciaVista(11, TipoDeber.Comida, new TimeOnly(8, 0), Pendiente: true),
                new InstanciaVista(10, TipoDeber.Paseo, new TimeOnly(18, 0), Pendiente: false),
            ],
            Seccion(estado, Firulais).Instancias);
        Assert.Empty(Seccion(estado, Michi).Instancias);
    }

    [Fact]
    public void Armar_DosPaseosYComidaHoy_ResumenCorrecto()
    {
        IReadOnlyList<RegistroLeido> registros =
        [
            new(Firulais.Id, TipoDeber.Paseo, Hoy, new TimeOnly(11, 0)),
            new(Firulais.Id, TipoDeber.Comida, Ayer, new TimeOnly(20, 0)),
            new(Firulais.Id, TipoDeber.Paseo, Hoy, new TimeOnly(9, 0)),
            new(Firulais.Id, TipoDeber.Comida, Hoy, new TimeOnly(13, 0)),
            new(Firulais.Id, TipoDeber.Paseo, Ayer, new TimeOnly(21, 0)),
            new(Michi.Id, TipoDeber.Paseo, Hoy, new TimeOnly(15, 0)),
        ];

        var estado = ArmadoEstadoDelDia.Armar(Mascotas, [], registros, A(Hoy, 16, 0));

        var firulais = Seccion(estado, Firulais);
        Assert.Equal(2, firulais.PaseosHoy);
        Assert.Equal(new UltimoRegistro(Hoy, new TimeOnly(11, 0)), firulais.UltimoPaseo);
        Assert.Equal(new UltimoRegistro(Hoy, new TimeOnly(13, 0)), firulais.UltimaComida);
        Assert.Equal(1, Seccion(estado, Michi).PaseosHoy);
        Assert.Null(Seccion(estado, Michi).UltimaComida);
    }

    [Fact]
    public void Armar_RegistrosDeAyer_MuestraFechasDeAyerYCeroPaseos()
    {
        IReadOnlyList<RegistroLeido> registros =
        [
            new(Firulais.Id, TipoDeber.Comida, Ayer, new TimeOnly(20, 0)),
            new(Firulais.Id, TipoDeber.Paseo, Ayer, new TimeOnly(21, 0)),
            new(Firulais.Id, TipoDeber.Paseo, Ayer, new TimeOnly(7, 30)),
        ];

        var estado = ArmadoEstadoDelDia.Armar(Mascotas, [], registros, A(Hoy, 7, 0));

        var firulais = Seccion(estado, Firulais);
        Assert.Equal(0, firulais.PaseosHoy);
        Assert.Equal(new UltimoRegistro(Ayer, new TimeOnly(20, 0)), firulais.UltimaComida);
        Assert.Equal(new UltimoRegistro(Ayer, new TimeOnly(21, 0)), firulais.UltimoPaseo);
    }

    [Fact]
    public void Armar_SinInstancias_SeccionConSinTareasPendientes()
    {
        IReadOnlyList<InstanciaLeida> instancias =
        [
            new(10, Firulais.Id, TipoDeber.Comida, Hoy, new TimeOnly(8, 0), TieneRegistro: false),
        ];

        var estado = ArmadoEstadoDelDia.Armar(Mascotas, instancias, [], A(Hoy, 9, 0));

        var michi = Seccion(estado, Michi);
        Assert.Equal("Michi", michi.Nombre);
        Assert.True(michi.SinTareasPendientes);
        Assert.Equal(0, michi.PaseosHoy);
        Assert.Null(michi.UltimaComida);
        Assert.Null(michi.UltimoPaseo);
        Assert.False(Seccion(estado, Firulais).SinTareasPendientes);
    }

    [Fact]
    public void Armar_ExcluyeInstanciasDeAyerYCumplidas()
    {
        IReadOnlyList<InstanciaLeida> instancias =
        [
            new(10, Firulais.Id, TipoDeber.Comida, Ayer, new TimeOnly(8, 0), TieneRegistro: false),
            new(11, Firulais.Id, TipoDeber.Comida, Hoy, new TimeOnly(8, 0), TieneRegistro: true),
            new(12, Firulais.Id, TipoDeber.Paseo, Hoy, new TimeOnly(9, 0), TieneRegistro: false),
        ];

        var estado = ArmadoEstadoDelDia.Armar(Mascotas, instancias, [], A(Hoy, 10, 0));

        Assert.Equal([12], Seccion(estado, Firulais).Instancias.Select(i => i.Id));
    }

    [Fact]
    public void Armar_OrdenaPorHora()
    {
        IReadOnlyList<InstanciaLeida> instancias =
        [
            new(10, Firulais.Id, TipoDeber.Paseo, Hoy, new TimeOnly(20, 0), TieneRegistro: false),
            new(11, Firulais.Id, TipoDeber.Paseo, Hoy, new TimeOnly(8, 0), TieneRegistro: false),
            new(12, Firulais.Id, TipoDeber.Comida, Hoy, new TimeOnly(13, 30), TieneRegistro: false),
            new(13, Firulais.Id, TipoDeber.Comida, Hoy, new TimeOnly(8, 0), TieneRegistro: false),
        ];

        var estado = ArmadoEstadoDelDia.Armar(Mascotas, instancias, [], A(Hoy, 6, 0));

        Assert.Equal([13, 11, 12, 10], Seccion(estado, Firulais).Instancias.Select(i => i.Id));
    }

    [Fact]
    public void Armar_SinMascotas_CeroSecciones()
    {
        IReadOnlyList<InstanciaLeida> instancias =
        [
            new(10, Firulais.Id, TipoDeber.Comida, Hoy, new TimeOnly(8, 0), TieneRegistro: false),
        ];
        IReadOnlyList<RegistroLeido> registros = [new(Firulais.Id, TipoDeber.Paseo, Hoy, new TimeOnly(7, 0))];

        // Control: las secciones salen de las mascotas, no de las instancias ni de los registros.
        var soloMichi = ArmadoEstadoDelDia.Armar([Michi], instancias, registros, A(Hoy, 9, 0));
        var sinMascotas = ArmadoEstadoDelDia.Armar([], instancias, registros, A(Hoy, 9, 0));

        Assert.Equal([Michi.Id], soloMichi.Secciones.Select(s => s.MascotaId));
        Assert.Empty(sinMascotas.Secciones);
    }

    private static DateTime A(DateOnly dia, int hora, int minuto) => dia.ToDateTime(new TimeOnly(hora, minuto));

    private static SeccionMascota Seccion(EstadoDelDia estado, MascotaVista mascota) =>
        Assert.Single(estado.Secciones, s => s.MascotaId == mascota.Id);
}
