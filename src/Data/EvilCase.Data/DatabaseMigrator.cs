using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Data;

internal sealed class DatabaseMigrator(ApplicationDbContext dbContext, ILogger<DatabaseMigrator> logger) : IDatabaseMigrator
{
    public async Task Migrate(CancellationToken token)
    {
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(token)).ToList();

        if (pendingMigrations.Count == 0)
        {
            logger.LogInformation("Database schema is up to date");
            return;
        }

        logger.LogInformation(
            "Applying {PendingMigrationCount} pending database migrations: {PendingMigrations}",
            pendingMigrations.Count,
            string.Join(", ", pendingMigrations));

        await dbContext.Database.MigrateAsync(token);

        logger.LogInformation("Applied {PendingMigrationCount} database migrations", pendingMigrations.Count);
    }
}
