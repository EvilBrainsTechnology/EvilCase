using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvilBrains.EvilCase.Data.Interceptors;

/// <summary>
/// Fills a new tenant row's <c>TenantId</c> from <see cref="ITenantContext"/>. Refuses a row that
/// already carries another tenant's id.
/// </summary>
internal sealed class TenantWriteInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        this.Apply(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        this.Apply(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        var entries = context?.ChangeTracker.Entries()
            .Where(entry => entry.Entity is ITenantEntity)
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        // The tenant is read only where the write touches a tenant row, so signing in still writes.
        if (entries is not { Count: > 0 })
            return;

        var tenantId = tenantContext.TenantId;

        foreach (var entry in entries)
        {
            var property = entry.Property(nameof(ITenantEntity.TenantId));

            if (entry.State == EntityState.Added && (Guid)property.CurrentValue! == Guid.Empty)
            {
                property.CurrentValue = tenantId;
                continue;
            }

            if ((Guid)property.CurrentValue! != tenantId)
            {
                throw new InvalidOperationException(
                    $"{entry.Metadata.DisplayName()} is written under tenant {tenantId} but carries {property.CurrentValue}");
            }
        }
    }
}
