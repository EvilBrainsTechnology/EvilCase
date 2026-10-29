using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Data;

public static class UniqueViolations
{
    public static bool IsUniqueViolation(this DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }

    /// <summary>
    /// A bulk update or delete surfaces the provider's own exception rather than a DbUpdateException.
    /// </summary>
    public static bool IsUniqueViolation(this DbException exception)
    {
        return exception is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
