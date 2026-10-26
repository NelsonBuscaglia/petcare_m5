using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Time.Testing;
using PetCare.Features.Deberes.Data;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Emails.Data;
using PetCare.Features.Emails.Domain;
using PetCare.Features.Instancias.Data;
using PetCare.Features.Instancias.Domain;
using PetCare.Shared.Data;
using PetCare.Shared.Hosting;
using PetCare.Shared.Tiempo;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.Emails;

public sealed class EmailsVencimientoServiceTests : IDisposable
{
    private const int Firulais = 1;
    private const int Michi = 2;

    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly DateOnly Ayer = Hoy.AddDays(-1);
    private static readonly TimeOnly Ocho = new(8, 0);
    private static readonly TimeOnly Nueve = new(9, 0);
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(5);

    private readonly BaseDeTest baseDeTest = BaseDeTest.Crear();
    /// <summary>Arranca a las 00:00 locales de hoy: el reloj falso solo avanza.</summary>
    private readonly FakeTimeProvider reloj = new(Local(0, 0));
    private readonly EnvioEmailFalso envio;

    public EmailsVencimientoServiceTests()
    {
        envio = new EnvioEmailFalso(reloj);
    }

    public void Dispose() => baseDeTest.Dispose();

    [Fact]
    public async Task Procesar_0915SinCumplir_EnviaAAnaYBeto()
    {
        var instancia = InsertarDeHoy(Firulais, TipoDeber.Comida, Nueve);
        IrA(9, 15, 30);

        var resumen = await Procesar();

        var intento = Assert.Single(envio.Intentos);
        Assert.Equal(ResultadoDelIntento.Aceptado, intento.Resultado);
        Assert.Equal(Local(9, 15, 30), intento.Instante);
        Assert.Equal(["ana@ejemplo.com", "beto@ejemplo.com"], intento.Mensaje.Para);
        Assert.Contains("Firulais", intento.Mensaje.Asunto);
        Assert.Equal([new EnvioAceptado(instancia.Id, YaRegistrado: false)], resumen.Aceptados);
        Assert.Empty(resumen.Fallidos);
        var email = Assert.Single(await Emails());
        Assert.Equal(instancia.Id, email.InstanciaId);
        Assert.Equal(Local(9, 15, 30), email.AceptadoEnUtc);
        Assert.Equal(TimeSpan.Zero, email.AceptadoEnUtc.Offset);
    }

    [Fact]
    public async Task Procesar_Cumplida0905_NoEnvia()
    {
        var cumplida = InsertarDeHoy(Firulais, TipoDeber.Comida, Nueve);
        baseDeTest.InsertarRegistro(cumplida.Id, Hoy, new TimeOnly(9, 5));
        var control = InsertarDeHoy(Michi, TipoDeber.Comida, Nueve);

        await PasadasCadaMinuto(Local(9, 0), Local(9, 20));

        Assert.All(envio.Intentos, i => Assert.Contains("Michi", i.Mensaje.Asunto));
        Assert.Single(envio.Aceptados);
        Assert.Equal(control.Id, Assert.Single(await Emails()).InstanciaId);
    }

    [Fact]
    public async Task Procesar_TodoElDia_ExactamenteUnEmail()
    {
        var instancia = InsertarDeHoy(Firulais, TipoDeber.Paseo, Nueve);

        await PasadasCadaMinuto(Local(9, 0), Local(23, 59));

        var intento = Assert.Single(envio.Intentos);
        Assert.Equal(Local(9, 15), intento.Instante);
        Assert.Equal(instancia.Id, Assert.Single(await Emails()).InstanciaId);
    }

    [Fact]
    public async Task Procesar_EliminarRegistroA1000_EnviaAntesDe1005()
    {
        var instancia = InsertarDeHoy(Firulais, TipoDeber.Comida, Ocho);
        IrA(8, 10);
        var registroId = await Registrar(instancia.Id, "08:10");
        await PasadasCadaMinuto(Local(8, 10), Local(10, 0));
        Assert.Empty(envio.Intentos);

        await Eliminar(registroId);
        await PasadasCadaMinuto(Local(10, 1), Local(10, 5));

        var intento = Assert.Single(envio.Intentos);
        Assert.Equal(ResultadoDelIntento.Aceptado, intento.Resultado);
        Assert.Equal(Local(10, 1), intento.Instante);
        Assert.Equal(instancia.Id, Assert.Single(await Emails()).InstanciaId);
    }

    [Fact]
    public async Task Procesar_EmailYaAceptado_NoReenviaTrasEliminar()
    {
        var instancia = InsertarDeHoy(Firulais, TipoDeber.Comida, Ocho);
        await PasadasCadaMinuto(Local(8, 0), Local(8, 15));
        Assert.Equal(Local(8, 15), Assert.Single(envio.Aceptados).Instante);

        IrA(9, 0);
        var registroId = await Registrar(instancia.Id, "09:00");
        IrA(10, 0);
        await Eliminar(registroId);
        await PasadasCadaMinuto(Local(10, 0), Local(23, 59));

        Assert.Single(envio.Intentos);
        Assert.Single(await Emails());
    }

    [Fact]
    public async Task Procesar_AltaConHoraVencida_NoEnvia()
    {
        IrA(10, 30);
        var vencida = await DarDeAlta(Firulais, "paseo", "10:00");
        var control = await DarDeAlta(Michi, "comida", "10:30");

        await PasadasCadaMinuto(Local(10, 30), Local(23, 59));

        var intento = Assert.Single(envio.Intentos);
        Assert.Contains("Michi", intento.Mensaje.Asunto);
        Assert.Equal(Local(10, 45), intento.Instante);
        var email = Assert.Single(await Emails());
        Assert.NotEqual(await InstanciaDe(vencida), email.InstanciaId);
        Assert.Equal(await InstanciaDe(control), email.InstanciaId);
    }

    [Fact]
    public async Task Procesar_GeneradaTrasCaida_NoEnvia()
    {
        baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Ocho, Repeticion.TodosLosDias, Ayer);
        baseDeTest.InsertarDeber(Michi, TipoDeber.Comida, new TimeOnly(11, 0), Repeticion.TodosLosDias, Ayer);
        IrA(10, 0);
        await using (var contexto = baseDeTest.CrearContexto())
        {
            var generadas = await new InstanciasService(contexto, reloj, new GeneracionLock()).GenerarDelDiaAsync(CancellationToken.None);
            Assert.Equal(2, generadas.Valor);
        }

        await PasadasCadaMinuto(Local(10, 0), Local(23, 59));

        var intento = Assert.Single(envio.Intentos);
        Assert.Contains("Michi", intento.Mensaje.Asunto);
        Assert.Equal(Local(11, 15), intento.Instante);
    }

    [Fact]
    public async Task Procesar_RechazaHasta0917_AceptadoAntesDe0920()
    {
        var instancia = InsertarDeHoy(Firulais, TipoDeber.Comida, Nueve);
        envio.RechazarHasta(Local(9, 17));

        var resumenes = await PasadasCadaMinuto(Local(9, 15), Local(9, 20));

        Assert.Equal(
            [(Local(9, 15), ResultadoDelIntento.Rechazado), (Local(9, 16), ResultadoDelIntento.Rechazado), (Local(9, 17), ResultadoDelIntento.Aceptado)],
            envio.Intentos.Select(i => (i.Instante, i.Resultado)));
        Assert.Equal(new EnvioFallido(instancia.Id, EnvioEmailError.Rechazado), Assert.Single(resumenes[0].Fallidos));
        Assert.Equal(Local(9, 17), Assert.Single(await Emails()).AceptadoEnUtc);
    }

    [Fact]
    public async Task Procesar_RechazaHasta0930_SinIntentosDespuesDe0920()
    {
        InsertarDeHoy(Firulais, TipoDeber.Comida, Nueve);
        envio.RechazarHasta(Local(9, 30));

        await PasadasCadaMinuto(Local(9, 15), Local(9, 40));

        Assert.Equal(
            [Local(9, 15), Local(9, 16), Local(9, 17), Local(9, 18), Local(9, 19)],
            envio.Intentos.Select(i => i.Instante));
        Assert.All(envio.Intentos, i => Assert.Equal(ResultadoDelIntento.Rechazado, i.Resultado));
        Assert.Empty(await Emails());
    }

    [Fact]
    public async Task Procesar_VentanaPerdidaYReactivada1030_EnviaAntesDe1035()
    {
        var instancia = InsertarDeHoy(Firulais, TipoDeber.Comida, Nueve);
        envio.RechazarHasta(Local(9, 30));
        await PasadasCadaMinuto(Local(9, 15), Local(10, 0));
        Assert.Empty(envio.Aceptados);

        var registroId = await Registrar(instancia.Id, "10:00");
        IrA(10, 30);
        await Eliminar(registroId);
        await PasadasCadaMinuto(Local(10, 30), Local(10, 35));

        var aceptado = Assert.Single(envio.Aceptados);
        Assert.True(aceptado.Instante <= Local(10, 35), $"Aceptado a las {ZonaHogar.AhoraLocal(aceptado.Instante):HH:mm}.");
        Assert.Equal(Local(10, 30), aceptado.Instante);
        Assert.Equal(instancia.Id, Assert.Single(await Emails()).InstanciaId);
    }

    [Fact]
    public async Task Procesar_SinRespuesta_FallaA30sYAceptaAntesDe0920()
    {
        var instancia = InsertarDeHoy(Firulais, TipoDeber.Comida, Nueve);
        envio.NoResponderHasta(Local(9, 17));

        foreach (var minuto in new[] { 15, 16 })
        {
            IrA(9, minuto);
            var intentosPrevios = envio.Intentos.Count;
            var pasada = Procesar();
            Assert.True(await EsperarAsync(() => envio.Intentos.Count == intentosPrevios + 1), "El intento no empezó.");

            reloj.Advance(TimeSpan.FromSeconds(29));
            await Task.Delay(50);
            Assert.False(pasada.IsCompleted, $"A las 09:{minuto}:29 el intento ya había terminado.");

            reloj.Advance(TimeSpan.FromSeconds(1));
            Assert.True(await EsperarAsync(() => pasada.IsCompleted), "El intento no falló a los 30 s.");
            var resumen = await pasada;
            Assert.Equal(new EnvioFallido(instancia.Id, EnvioEmailError.SinRespuesta), Assert.Single(resumen.Fallidos));
            Assert.Empty(resumen.Aceptados);
            Assert.Empty(await Emails());
        }

        IrA(9, 17);
        var aceptada = await Procesar();

        Assert.Equal([new EnvioAceptado(instancia.Id, YaRegistrado: false)], aceptada.Aceptados);
        Assert.Equal(
            [ResultadoDelIntento.SinRespuesta, ResultadoDelIntento.SinRespuesta, ResultadoDelIntento.Aceptado],
            envio.Intentos.Select(i => i.Resultado));
        Assert.True(Assert.Single(await Emails()).AceptadoEnUtc <= Local(9, 20));
    }

    [Fact]
    public async Task Procesar_PrimerEnvioLentoDejaFueraDeVentanaALaSegunda_NoLaIntenta()
    {
        // Firulais vence a las 09:15 (ventana hasta 09:20); Michi a las 09:11 (ventana hasta 09:16).
        // Las dos son candidatas a las 09:15:30, pero el intento de Firulais (menor id, va primero)
        // se cuelga 30 s: a las 09:16:00 la ventana de Michi ya cerró.
        var lenta = InsertarDeHoy(Firulais, TipoDeber.Comida, Nueve);
        InsertarDeHoy(Michi, TipoDeber.Comida, new TimeOnly(8, 56));
        envio.NoResponderHasta(Local(9, 15, 31));
        IrA(9, 15, 30);

        var pasada = Procesar();
        Assert.True(await EsperarAsync(() => envio.Intentos.Count == 1), "El primer intento no empezó.");
        reloj.Advance(EmailsVencimientoService.TimeoutPorIntento);
        Assert.True(await EsperarAsync(() => pasada.IsCompleted), "El primer intento no falló a los 30 s.");
        var resumen = await pasada;

        var intento = Assert.Single(envio.Intentos);
        Assert.Contains("Firulais", intento.Mensaje.Asunto);
        Assert.Equal(new EnvioFallido(lenta.Id, EnvioEmailError.SinRespuesta), Assert.Single(resumen.Fallidos));
        Assert.Empty(resumen.Aceptados);
        Assert.Empty(await Emails());
    }

    [Fact]
    public async Task Procesar_CancelacionDelLlamadorDuranteIntento_Propaga()
    {
        InsertarDeHoy(Firulais, TipoDeber.Comida, Nueve);
        envio.NoResponderHasta(Local(9, 17));
        IrA(9, 15);
        using var apagado = new CancellationTokenSource();

        var pasada = ProcesarConToken(apagado.Token);
        Assert.True(await EsperarAsync(() => envio.Intentos.Count == 1), "El intento no empezó.");
        await apagado.CancelAsync();

        // Se apaga el host, no venció el plazo del intento: no es SinRespuesta, se propaga.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pasada);
        Assert.Empty(await Emails());
    }

    [Fact]
    public async Task Procesar_ChoqueAlGuardarEmail_ErrorTipadoSinReintento()
    {
        var instancia = InsertarDeHoy(Firulais, TipoDeber.Comida, Nueve);
        IrA(9, 15);
        var interceptor = new InsertaAntesDeGuardar(() => baseDeTest.InsertarEmailVencimiento(instancia.Id, Local(9, 15)));

        var resumen = await Procesar(interceptor);

        Assert.True(interceptor.Inserto);
        Assert.Equal([new EnvioAceptado(instancia.Id, YaRegistrado: true)], resumen.Aceptados);
        Assert.Empty(resumen.Fallidos);
        Assert.Single(await Emails());

        IrA(9, 16);
        var siguiente = await Procesar();
        Assert.Empty(siguiente.Aceptados);
        Assert.Single(envio.Intentos);
    }

    [Fact]
    public async Task Procesar_ExcepcionNoClasificada_Propaga()
    {
        InsertarDeHoy(Firulais, TipoDeber.Comida, Nueve);
        IrA(9, 15);
        var excepcion = new InvalidOperationException("falla-no-clasificada");
        envio.LanzarEnElProximoIntento(excepcion);

        var lanzada = await Assert.ThrowsAsync<InvalidOperationException>(() => Procesar());

        Assert.Same(excepcion, lanzada);
        Assert.Empty(await Emails());
    }

    private Instancia InsertarDeHoy(int mascotaId, TipoDeber tipo, TimeOnly hora)
    {
        var deber = baseDeTest.InsertarDeber(mascotaId, tipo, hora, Repeticion.TodosLosDias, Hoy);
        return baseDeTest.InsertarInstancia(deber.Id, Hoy, hora);
    }

    private async Task<ResumenEnvios> Procesar(params IInterceptor[] interceptores)
    {
        await using var contexto = new PetCareDbContext(
            new DbContextOptionsBuilder<PetCareDbContext>()
                .UseSqlite(baseDeTest.Conexion)
                .AddInterceptors(interceptores)
                .Options);
        return await new EmailsVencimientoService(contexto, envio, reloj).ProcesarAsync(CancellationToken.None);
    }

    private async Task<ResumenEnvios> ProcesarConToken(CancellationToken cancellationToken)
    {
        await using var contexto = baseDeTest.CrearContexto();
        return await new EmailsVencimientoService(contexto, envio, reloj).ProcesarAsync(cancellationToken);
    }

    /// <summary>Una pasada por minuto, de <paramref name="desde"/> a <paramref name="hasta"/> inclusive.</summary>
    private async Task<List<ResumenEnvios>> PasadasCadaMinuto(DateTimeOffset desde, DateTimeOffset hasta)
    {
        var resumenes = new List<ResumenEnvios>();
        for (var instante = desde; instante <= hasta; instante = instante.AddMinutes(1))
        {
            if (instante > reloj.GetUtcNow())
            {
                reloj.SetUtcNow(instante);
            }

            resumenes.Add(await Procesar());
        }

        return resumenes;
    }

    private async Task<int> Registrar(int instanciaId, string hora)
    {
        await using var contexto = baseDeTest.CrearContexto();
        var resultado = await new RegistrosService(contexto, reloj).RegistrarAsync(instanciaId, hora, CancellationToken.None);
        Assert.True(resultado.EsOk, $"No se registró: {string.Join(", ", resultado.Errores)}");
        return resultado.Valor;
    }

    private async Task Eliminar(int registroId)
    {
        await using var contexto = baseDeTest.CrearContexto();
        var resultado = await new RegistrosService(contexto, reloj).EliminarAsync(registroId, CancellationToken.None);
        Assert.True(resultado.EsOk, $"No se eliminó: {string.Join(", ", resultado.Errores)}");
    }

    private async Task<int> DarDeAlta(int mascotaId, string tipo, string hora)
    {
        await using var contexto = baseDeTest.CrearContexto();
        var resultado = await new DeberesService(contexto, reloj, new GeneracionLock())
            .DarDeAltaAsync(mascotaId, tipo, hora, "solo-hoy", CancellationToken.None);
        Assert.True(resultado.EsOk, $"No se dio de alta: {string.Join(", ", resultado.Errores)}");
        return resultado.Valor;
    }

    private async Task<int> InstanciaDe(int deberId)
    {
        await using var contexto = baseDeTest.CrearContexto();
        return await contexto.Instancias.Where(i => i.DeberId == deberId).Select(i => i.Id).SingleAsync();
    }

    private async Task<List<EmailVencimiento>> Emails()
    {
        await using var contexto = baseDeTest.CrearContexto();
        return await contexto.EmailsVencimiento.ToListAsync();
    }

    private void IrA(int hora, int minuto, int segundo = 0) => reloj.SetUtcNow(Local(hora, minuto, segundo));

    private static DateTimeOffset Local(int hora, int minuto, int segundo = 0) =>
        ZonaHogar.AUtc(Hoy.ToDateTime(new TimeOnly(hora, minuto, segundo)));

    /// <summary>Espera en tiempo real (el reloj falso no avanza solo).</summary>
    private static async Task<bool> EsperarAsync(Func<bool> condicion)
    {
        var cronometro = Stopwatch.StartNew();
        while (cronometro.Elapsed < EsperaMaxima)
        {
            if (condicion())
            {
                return true;
            }

            await Task.Delay(10);
        }

        return condicion();
    }
}
