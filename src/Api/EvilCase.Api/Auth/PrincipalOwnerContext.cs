using System.Security.Claims;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Data;
using Microsoft.AspNetCore.Http;

namespace EvilBrains.EvilCase.Api.Auth;

internal sealed class PrincipalOwnerContext(IHttpContextAccessor httpContextAccessor) : IOwnerContext
{
    public long OwnerId =>
        this.OwnerIdOrDefault ?? throw new InvalidOperationException("The request has no authenticated owner.");

    public long? OwnerIdOrDefault
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirstValue(AuthClaims.Subject);

            return long.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var ownerId)
                ? ownerId
                : null;
        }
    }
}
