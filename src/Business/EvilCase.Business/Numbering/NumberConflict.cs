using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Tells a race for a number apart from every other failed write.
/// </summary>
internal static class NumberConflict
{
    /// <summary>
    /// The unique index a raced case number breaks.
    /// </summary>
    public const string CaseNumberIndex = "IX_Cases_TenantId_CaseNumber";

    public const string ActNumberIndex = "IX_Acts_TenantId_ActNumber";

    public static bool IsCaseNumberConflict(DbUpdateException exception) => IsConflictOn(exception, CaseNumberIndex);

    public static bool IsActNumberConflict(DbUpdateException exception) => IsConflictOn(exception, ActNumberIndex);

    private static bool IsConflictOn(DbUpdateException exception, string index) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
            && string.Equals(postgres.ConstraintName, index, StringComparison.Ordinal);
}
