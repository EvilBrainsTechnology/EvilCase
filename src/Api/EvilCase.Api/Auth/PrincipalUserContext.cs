using System.Security.Claims;
using EvilBrains.Dispose;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.AspNetCore.Http;

namespace EvilBrains.EvilCase.Api.Auth;

internal sealed class PrincipalUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private Guid? entered;

    public Guid UserId => this.UserIdOrDefault ?? throw new InvalidOperationException("The request has no signed-in user.");

    public Guid? UserIdOrDefault
    {
        get
        {
            if (this.entered is { } scoped)
                return scoped;

            var claim = httpContextAccessor.HttpContext?.User.FindFirstValue(AuthClaims.Subject);

            return Guid.TryParse(claim, CultureInfo.InvariantCulture, out var userId) ? userId : null;
        }
    }

    public IDisposable Enter(Guid userId)
    {
        var previous = this.entered;
        this.entered = userId;

        return new ActionDisposableScope(() => this.entered = previous);
    }
}
