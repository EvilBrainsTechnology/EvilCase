using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvilBrains.EvilCase.Data.Interceptors;

internal sealed class TenantWriteInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        this.Verify(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        this.Verify(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // The tenant is read only once a tenant row is actually being written: signing in writes a refresh
    // token, and that carries no tenant at all.
    private void Verify(DbContext? context)
    {
        if (context is null)
            return;

        var entries = context.ChangeTracker
            .Entries<ITenantEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
            return;

        var tenantId = tenantContext.TenantId;

        foreach (var entry in entries)
        {
            if (entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    $"A {entry.Metadata.ShortName()} of tenant {entry.Entity.TenantId} was written outside that tenant.");
            }
        }
    }
}
