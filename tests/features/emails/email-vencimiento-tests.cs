using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PetCare.Features.Deberes.Domain;
using PetCare.Features.Instancias.Domain;
using PetCare.Shared.Data;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Features.Emails;

public sealed class EmailVencimientoTests : IDisposable
{
    private const int Firulais = 1;

    private static readonly DateOnly Hoy = new(2026, 10, 25);
    private static readonly TimeOnly Nueve = new(9, 0);

    /// <summary>09:15 local del 25/10 = 12:15 UTC.</summary>
    private static readonly DateTimeOffset Aceptado = new(2026, 10, 25, 12, 15, 30, TimeSpan.Zero);

    private readonly BaseDeTest baseDeTest = BaseDeTest.Crear();

    public void Dispose() => baseDeTest.Dispose();

    [Fact]
    public async Task EmailVencimiento_AceptadoEnUtc_SePersisteConOffsetCero()
    {
        var instancia = InsertarInstanciaDeHoy();

        var email = baseDeTest.InsertarEmailVencimiento(instancia.Id, Aceptado);

        await using var contexto = baseDeTest.CrearContexto();
        var leido = await contexto.EmailsVencimiento.SingleAsync();
        Assert.Equal(email.Id, leido.Id);
        Assert.Equal(instancia.Id, leido.InstanciaId);
        Assert.Equal(Aceptado, leido.AceptadoEnUtc);
        Assert.Equal(TimeSpan.Zero, leido.AceptadoEnUtc.Offset);

        // Se guarda como TEXT con el offset explícito: nunca se compara en SQL.
        using var comando = baseDeTest.Conexion.CreateCommand();
        comando.CommandText = "SELECT AceptadoEnUtc FROM EmailsVencimiento";
        var texto = Assert.IsType<string>(comando.ExecuteScalar());
        Assert.EndsWith("+00:00", texto);
    }

    [Fact]
    public void EmailVencimiento_DosParaLaMismaInstancia_ViolaIndiceUnico()
    {
        var instancia = InsertarInstanciaDeHoy();
        baseDeTest.InsertarEmailVencimiento(instancia.Id, Aceptado);

        var ex = Assert.Throws<DbUpdateException>(() => baseDeTest.InsertarEmailVencimiento(instancia.Id, Aceptado.AddMinutes(1)));

        var sqlite = Assert.IsType<SqliteException>(ex.InnerException);
        Assert.Equal(SqliteErrores.ConstraintUnique, sqlite.SqliteExtendedErrorCode);
        Assert.True(SqliteErrores.EsViolacionUnica(ex));

        // Sin SqliteException adentro (otro proveedor o un error de EF) no es un choque.
        Assert.False(SqliteErrores.EsViolacionUnica(new DbUpdateException("otra", new InvalidOperationException())));
        Assert.False(SqliteErrores.EsViolacionUnica(new DbUpdateException("sin inner")));

        // Una FK rota también es SQLITE_CONSTRAINT (19), pero no es un choque del índice único.
        var fk = Assert.Throws<DbUpdateException>(() => baseDeTest.InsertarEmailVencimiento(999, Aceptado));
        Assert.False(SqliteErrores.EsViolacionUnica(fk));
        using var contexto = baseDeTest.CrearContexto();
        Assert.Equal(1, contexto.EmailsVencimiento.Count());
    }

    private Instancia InsertarInstanciaDeHoy()
    {
        var deber = baseDeTest.InsertarDeber(Firulais, TipoDeber.Comida, Nueve, Repeticion.TodosLosDias, Hoy);
        return baseDeTest.InsertarInstancia(deber.Id, Hoy, Nueve);
    }
}
