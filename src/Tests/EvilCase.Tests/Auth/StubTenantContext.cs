using EvilBrains.Dispose;
using EvilBrains.EvilCase.Domain.Tenancy;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// A minimal <see cref="ITenantContext"/> for tests that only need <see cref="Enter"/> to work.
/// </summary>
internal sealed class StubTenantContext : ITenantContext
{
    private Guid? entered;

    public Guid TenantId => this.TenantIdOrDefault ?? throw new InvalidOperationException("The request has no tenant.");

    public Guid? TenantIdOrDefault => this.entered;

    public IDisposable Enter(Guid tenantId)
    {
        var previous = this.entered;
        this.entered = tenantId;

        return new ActionDisposableScope(() => this.entered = previous);
    }
}
