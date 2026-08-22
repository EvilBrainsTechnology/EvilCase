using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Tenancy;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvilBrains.EvilCase.Data.Interceptors;

/// <summary>
/// Fills a new tenant row's <c>TenantId</c> from <see cref="ITenantContext"/> and a new user-owned row's
/// <c>UserId</c> from <see cref="IUserContext"/>. Refuses a row that already carries another tenant's or
/// another user's id.
/// </summary>
internal sealed class TenantWriteInterceptor(ITenantContext tenantContext, IUserContext userContext) : SaveChangesInterceptor
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
            ApplyTenant(entry, tenantId);
            this.ApplyUser(entry);
        }
    }

    private static void ApplyTenant(EntityEntry entry, in Guid tenantId)
    {
        var property = entry.Property(nameof(ITenantEntity.TenantId));

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
        if (entry.Entity is not IUserOwnedEntity)
            return;

        var property = entry.Property(nameof(IUserOwnedEntity.UserId));

        if (entry.State == EntityState.Added && (Guid)property.CurrentValue! == Guid.Empty)
        {
            property.CurrentValue = userContext.UserId;
            return;
        }

        // The startup seed writes rows it owns with no request behind it, so the owner is checked only
        // where a signed-in user exists.
        if (userContext.UserIdOrDefault is { } userId && (Guid)property.CurrentValue! != userId)
        {
            throw new InvalidOperationException(
                $"{entry.Metadata.DisplayName()} is written by user {userId} but carries {property.CurrentValue}");
        }
    }
}
