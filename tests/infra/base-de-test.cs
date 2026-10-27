using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Emails.Domain;
using PetCare.Features.Instancias.Domain;
using PetCare.Shared.Data;

namespace PetCare.Tests.Infra;

/// <summary>
/// Base SQLite en memoria, migrada, que vive mientras la conexión siga abierta.
/// Cada test crea la suya: no hay estado compartido entre tests.
/// </summary>
public sealed class BaseDeTest : IDisposable
{
    private readonly SqliteConnection conexion;

    private BaseDeTest(SqliteConnection conexion)
    {
        this.conexion = conexion;
    }

    public SqliteConnection Conexion => conexion;

    public static BaseDeTest Crear(bool migrar = true)
    {
        var conexion = new SqliteConnection("Data Source=:memory:");
        conexion.Open();
        var baseDeTest = new BaseDeTest(conexion);
        if (migrar)
        {
            using var contexto = baseDeTest.CrearContexto();
            contexto.Database.Migrate();
        }

        return baseDeTest;
    }

    public static DbContextOptions<PetCareDbContext> Opciones(SqliteConnection conexion) =>
        new DbContextOptionsBuilder<PetCareDbContext>().UseSqlite(conexion).Options;

    public PetCareDbContext CrearContexto() => new(Opciones(conexion));

    public Deber InsertarDeber(
        int mascotaId,
        TipoDeber tipo,
        TimeOnly hora,
        Repeticion repeticion,
        DateOnly fechaAlta)
    {
        using var contexto = CrearContexto();
        var deber = new Deber
        {
            MascotaId = mascotaId,
            Tipo = tipo,
            Hora = hora,
            Repeticion = repeticion,
            FechaAlta = fechaAlta,
        };
        contexto.Deberes.Add(deber);
        contexto.SaveChanges();
        return deber;
    }

    public Instancia InsertarInstancia(
        int deberId,
        DateOnly fecha,
        TimeOnly hora,
        bool vencidaAlGenerarse = false,
        DateTimeOffset? reactivadaEnUtc = null)
    {
        using var contexto = CrearContexto();
        var instancia = new Instancia
        {
            DeberId = deberId,
            Fecha = fecha,
            Hora = hora,
            VencidaAlGenerarse = vencidaAlGenerarse,
            ReactivadaEnUtc = reactivadaEnUtc,
        };
        contexto.Instancias.Add(instancia);
        contexto.SaveChanges();
        return instancia;
    }

    public RegistroCumplimiento InsertarRegistro(int instanciaId, DateOnly fecha, TimeOnly horaReal)
    {
        using var contexto = CrearContexto();
        var registro = new RegistroCumplimiento
        {
            InstanciaId = instanciaId,
            Fecha = fecha,
            HoraReal = horaReal,
        };
        contexto.RegistrosCumplimiento.Add(registro);
        contexto.SaveChanges();
        return registro;
    }

    public EmailVencimiento InsertarEmailVencimiento(int instanciaId, DateTimeOffset aceptadoEnUtc)
    {
        using var contexto = CrearContexto();
        var email = new EmailVencimiento { InstanciaId = instanciaId, AceptadoEnUtc = aceptadoEnUtc };
        contexto.EmailsVencimiento.Add(email);
        contexto.SaveChanges();
        return email;
    }

    public void Dispose() => conexion.Dispose();
}
