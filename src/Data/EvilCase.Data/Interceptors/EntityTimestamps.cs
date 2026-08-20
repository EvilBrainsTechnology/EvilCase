using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EvilBrains.EvilCase.Data.Interceptors;

internal static class EntityTimestamps
{
    public static void Apply(ChangeTracker changeTracker, in DateTime now)
    {
        foreach (var entry in changeTracker.Entries<IEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IEntity.Created)).CurrentValue = now;
                entry.Property(nameof(IEntity.Updated)).CurrentValue = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IEntity.Updated)).CurrentValue = now;
            }
        }
    }
}
