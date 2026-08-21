using System.Security.Claims;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.AspNetCore.Http;

namespace EvilBrains.EvilCase.Api.Auth;

internal sealed class PrincipalUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirstValue(AuthClaims.Subject);

            return Guid.TryParse(claim, CultureInfo.InvariantCulture, out var userId)
                ? userId
                : throw new InvalidOperationException("The request has no signed-in user.");
        }
    }
}
