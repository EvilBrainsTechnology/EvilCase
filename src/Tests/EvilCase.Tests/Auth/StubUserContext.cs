using EvilBrains.Dispose;
using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// A minimal <see cref="IUserContext"/>: nothing is resolved until <see cref="Enter"/> names both ids.
/// </summary>
internal sealed class StubUserContext : IUserContext
{
    private (Guid TenantId, Guid UserId)? entered;

    public Guid TenantId => this.TenantIdOrDefault ?? throw new InvalidOperationException("The request has no tenant.");

    public Guid? TenantIdOrDefault => this.entered?.TenantId;

    public Guid UserId => this.UserIdOrDefault ?? throw new InvalidOperationException("The request has no signed-in user.");

    public Guid? UserIdOrDefault => this.entered?.UserId;

    /// <summary>
    /// Every pair <see cref="Enter"/> was called with, in order.
    /// </summary>
    public List<(Guid TenantId, Guid UserId)> Entered { get; } = [];

    public IDisposable Enter(Guid tenantId, Guid userId)
    {
        var previous = this.entered;
        this.entered = (tenantId, userId);
        this.Entered.Add((tenantId, userId));

        return new ActionDisposableScope(() => this.entered = previous);
    }
}
