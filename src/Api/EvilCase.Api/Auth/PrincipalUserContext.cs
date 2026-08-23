using System.Security.Claims;
using EvilBrains.Dispose;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.AspNetCore.Http;

namespace EvilBrains.EvilCase.Api.Auth;

internal sealed class PrincipalUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private (Guid TenantId, Guid UserId)? entered;

    public Guid TenantId => this.TenantIdOrDefault ?? throw new InvalidOperationException("The request has no tenant.");

    public Guid? TenantIdOrDefault
    {
        get
        {
            if (this.entered is { } scoped)
                return scoped.TenantId;

            return this.Claim(AuthClaims.Tenant);
        }
    }

    public Guid UserId => this.UserIdOrDefault ?? throw new InvalidOperationException("The request has no signed-in user.");

    public Guid? UserIdOrDefault
    {
        get
        {
            if (this.entered is { } scoped)
                return scoped.UserId;

            return this.Claim(AuthClaims.Subject);
        }
    }

    public IDisposable Enter(Guid tenantId, Guid userId)
    {
        var previous = this.entered;
        this.entered = (tenantId, userId);

        return new ActionDisposableScope(() => this.entered = previous);
    }

    private Guid? Claim(string type)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(type);

        return Guid.TryParse(value, CultureInfo.InvariantCulture, out var claimId) ? claimId : null;
    }
}
