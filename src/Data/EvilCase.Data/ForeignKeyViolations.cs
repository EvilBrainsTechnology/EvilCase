using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Data;

/// <summary>
/// A key the model leaves at <see cref="DeleteBehavior.Restrict"/> is refused under its own error code.
/// </summary>
public static class ForeignKeyViolations
{
    public static bool IsForeignKeyViolation(this DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.RestrictViolation,
        };
    }
}
