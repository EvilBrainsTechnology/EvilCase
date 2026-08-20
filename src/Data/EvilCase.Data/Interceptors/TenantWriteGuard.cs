using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EvilBrains.EvilCase.Data.Interceptors;

internal static class TenantWriteGuard
{
    public static void Verify(ChangeTracker changeTracker, Guid? tenantId)
    {
        foreach (var entry in changeTracker.Entries<ITenantEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            if (tenantId is null || entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    $"A {entry.Metadata.ShortName()} of tenant {entry.Entity.TenantId} was written outside that tenant.");
            }
        }
    }
}
