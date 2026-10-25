using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PetCare.Shared.Data;

public static class SqliteErrores
{
    /// <summary>
    /// SQLITE_CONSTRAINT_UNIQUE: un índice único rechazó la escritura. El código base 19 también
    /// incluye FK, NOT NULL y CHECK, que no son un choque y deben propagarse.
    /// </summary>
    public const int ConstraintUnique = 2067;

    public static bool EsViolacionUnica(DbUpdateException ex) =>
        ex.InnerException is SqliteException { SqliteExtendedErrorCode: ConstraintUnique };
}
