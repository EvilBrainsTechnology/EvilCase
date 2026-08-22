using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Data;

/// <summary>
/// Reads a failed write for the one outcome a caller can act on: a unique index already holds the value.
/// </summary>
public static class UniqueViolations
{
    public static bool IsUniqueViolation(this DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
