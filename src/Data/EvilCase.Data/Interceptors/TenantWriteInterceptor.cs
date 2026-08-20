using EvilBrains.EvilCase.Domain.Tenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvilBrains.EvilCase.Data.Interceptors;

internal sealed class TenantWriteInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        this.Verify(eventData);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        this.Verify(eventData);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Verify(DbContextEventData eventData)
    {
        if (eventData.Context is { } context)
            TenantWriteGuard.Verify(context.ChangeTracker, tenantContext.TenantIdOrDefault);
    }
}
