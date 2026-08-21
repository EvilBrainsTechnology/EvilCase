using System.Security.Claims;
using EvilBrains.Dispose;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Domain.Tenancy;
using Microsoft.AspNetCore.Http;

namespace EvilBrains.EvilCase.Api.Auth;

internal sealed class PrincipalTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private Guid? entered;

    public Guid TenantId =>
        this.TenantIdOrDefault ?? throw new InvalidOperationException("The request has no tenant.");

    public Guid? TenantIdOrDefault
    {
        get
        {
            if (this.entered is { } scoped)
                return scoped;

            var claim = httpContextAccessor.HttpContext?.User.FindFirstValue(AuthClaims.Tenant);

            return Guid.TryParse(claim, CultureInfo.InvariantCulture, out var tenantId) ? tenantId : null;
        }
    }

    public Guid UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(AuthClaims.Subject), CultureInfo.InvariantCulture, out var userId)
            ? userId
            : throw new InvalidOperationException("The request has no user.");

    public IDisposable Enter(Guid tenantId)
    {
        var previous = this.entered;
        this.entered = tenantId;

        return new ActionDisposableScope(() => this.entered = previous);
    }
}
