using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Data;

/// <summary>
/// Reads a failed write for the one outcome a caller can act on: a foreign key still points at the row.
/// </summary>
public static class ForeignKeyViolations
{
    public static bool IsForeignKeyViolation(this DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
    }
}
