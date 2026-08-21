using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Tells the race for a number from every other failed write.
/// </summary>
internal static class NumberConflict
{
    public static bool IsTakenNumber(DbUpdateException exception, string column) =>
        exception.InnerException is PostgresException violation
            && IsTakenNumber(violation.SqlState, violation.ConstraintName, column);

    // The unique index of a number column is the one whose name ends with the column.
    public static bool IsTakenNumber(string? sqlState, string? constraintName, string column) =>
        string.Equals(sqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal)
            && constraintName?.EndsWith(column, StringComparison.Ordinal) == true;
}
