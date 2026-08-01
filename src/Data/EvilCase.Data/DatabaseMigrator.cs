using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Data;

internal sealed partial class DatabaseMigrator(ApplicationDbContext dbContext, ILogger<DatabaseMigrator> logger) : IDatabaseMigrator
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendingMigrations.Count == 0)
        {
            DatabaseUpToDate(logger);
            return;
        }

        ApplyingMigrations(logger, pendingMigrations.Count, string.Join(", ", pendingMigrations));

        await dbContext.Database.MigrateAsync(cancellationToken);

        MigrationsApplied(logger, pendingMigrations.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database schema is up to date")]
    private static partial void DatabaseUpToDate(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying {PendingMigrationCount} pending database migrations: {PendingMigrations}")]
    private static partial void ApplyingMigrations(ILogger logger, int pendingMigrationCount, string pendingMigrations);

    [LoggerMessage(Level = LogLevel.Information, Message = "Applied {PendingMigrationCount} database migrations")]
    private static partial void MigrationsApplied(ILogger logger, int pendingMigrationCount);
}
