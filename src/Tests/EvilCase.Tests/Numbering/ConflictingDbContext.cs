using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Tenancy;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// A real <see cref="ApplicationDbContext"/> that raises a unique-violation <see cref="DbUpdateException"/>
/// on its first <paramref name="conflicts"/> saves and succeeds after, without opening a connection.
/// </summary>
internal sealed class ConflictingDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantContext tenantContext,
    string constraintName,
    int conflicts) : ApplicationDbContext(options, tenantContext)
{
    public static ConflictingDbContext Create(string constraintName, int conflicts)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(npgsql => npgsql.UseEvilCaseMigrations());

        return new ConflictingDbContext(optionsBuilder.Options, new StubTenantContext(), constraintName, conflicts);
    }

    public int Saves { get; private set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.Saves++;

        if (this.Saves > conflicts)
            return Task.FromResult(1);

        var postgres = new PostgresException(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation,
            constraintName: constraintName);

        return Task.FromException<int>(new DbUpdateException("save failed", postgres));
    }
}
