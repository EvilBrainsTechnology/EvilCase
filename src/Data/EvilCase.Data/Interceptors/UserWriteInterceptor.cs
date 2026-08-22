using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvilBrains.EvilCase.Data.Interceptors;

/// <summary>
/// Fills a new row's <c>TenantId</c> and <c>UserId</c> from <see cref="IUserContext"/>. A row of another
/// tenant is refused; a row of another user in the same tenant is not, the tenant sees all of them.
/// </summary>
internal sealed class UserWriteInterceptor(IUserContext userContext) : SaveChangesInterceptor
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

        // The context is read only where the write touches a tenant row, so signing in still writes.
        if (entries is not { Count: > 0 })
            return;

        foreach (var entry in entries)
        {
            this.ApplyTenant(entry);

            if (entry.Entity is IUserOwnedEntity)
                this.ApplyUser(entry);
        }
    }

    private void ApplyTenant(EntityEntry entry)
    {
        var property = entry.Property(nameof(ITenantEntity.TenantId));
        var tenantId = userContext.TenantId;

        if (entry.State == EntityState.Added && (Guid)property.CurrentValue! == Guid.Empty)
        {
            property.CurrentValue = tenantId;
            return;
        }

        if ((Guid)property.CurrentValue! != tenantId)
        {
            throw new InvalidOperationException(
                $"{entry.Metadata.DisplayName()} is written under tenant {tenantId} but carries {property.CurrentValue}");
        }
    }

    private void ApplyUser(EntityEntry entry)
    {
        if (entry.State != EntityState.Added)
            return;

        var property = entry.Property(nameof(IUserOwnedEntity.UserId));

        if ((Guid)property.CurrentValue! == Guid.Empty)
            property.CurrentValue = userContext.UserId;
    }
}
