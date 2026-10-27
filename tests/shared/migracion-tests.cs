using Microsoft.Data.Sqlite;
using PetCare.Tests.Infra;

namespace PetCare.Tests.Shared;

public class MigracionTests
{
    [Fact]
    public void Migracion_AgregaColumnasYTablaEmails()
    {
        using var baseDeTest = BaseDeTest.Crear();

        var columnasInstancias = Columnas(baseDeTest.Conexion, "Instancias");
        Assert.True(columnasInstancias.TryGetValue("VencidaAlGenerarse", out var vencida), "Falta Instancias.VencidaAlGenerarse");
        Assert.Equal(("INTEGER", true), vencida);
        Assert.True(columnasInstancias.TryGetValue("ReactivadaEnUtc", out var reactivada), "Falta Instancias.ReactivadaEnUtc");
        Assert.Equal(("TEXT", false), reactivada);

        var columnasEmails = Columnas(baseDeTest.Conexion, "EmailsVencimiento");
        Assert.Equal(("INTEGER", true), columnasEmails["Id"]);
        Assert.Equal(("INTEGER", true), columnasEmails["InstanciaId"]);
        Assert.Equal(("TEXT", true), columnasEmails["AceptadoEnUtc"]);

        var unicos = IndicesUnicos(baseDeTest.Conexion, "EmailsVencimiento");
        Assert.Contains(["InstanciaId"], unicos);
    }

    private static Dictionary<string, (string Tipo, bool NotNull)> Columnas(SqliteConnection conexion, string tabla)
    {
        using var comando = conexion.CreateCommand();
        comando.CommandText = $"SELECT name, type, \"notnull\" FROM pragma_table_info('{tabla}')";
        using var lector = comando.ExecuteReader();
        var columnas = new Dictionary<string, (string, bool)>();
        while (lector.Read())
        {
            columnas[lector.GetString(0)] = (lector.GetString(1), lector.GetInt64(2) == 1);
        }

        return columnas;
    }

    private static List<List<string>> IndicesUnicos(SqliteConnection conexion, string tabla)
    {
        using var comando = conexion.CreateCommand();
        comando.CommandText = $"""
            SELECT il.name, ii.name
            FROM pragma_index_list('{tabla}') AS il
            JOIN pragma_index_info(il.name) AS ii
            WHERE il."unique" = 1 AND il.origin = 'c'
            ORDER BY il.name, ii.seqno
            """;
        using var lector = comando.ExecuteReader();
        var filas = new List<(string Indice, string Columna)>();
        while (lector.Read())
        {
            filas.Add((lector.GetString(0), lector.GetString(1)));
        }

        return filas.GroupBy(f => f.Indice).Select(g => g.Select(f => f.Columna).ToList()).ToList();
    }
}
