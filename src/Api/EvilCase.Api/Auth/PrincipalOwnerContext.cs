using System.Security.Claims;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Data;
using Microsoft.AspNetCore.Http;

namespace EvilBrains.EvilCase.Api.Auth;

/// <summary>
/// Resolves the owner from the access token's <c>sub</c> claim. The only place in the application that
/// reads it.
/// </summary>
internal sealed class PrincipalOwnerContext(IHttpContextAccessor httpContextAccessor) : IOwnerContext
{
    public long? OwnerId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirstValue(AuthClaims.Subject);

            // A token that carries no usable subject is treated as no caller rather than as an error:
            // the pipeline has already decided whether the request may proceed, and this only answers
            // whose it is.
            return long.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var ownerId)
                ? ownerId
                : null;
        }
    }

    public long RequireOwnerId() =>
        this.OwnerId ?? throw new InvalidOperationException("The request has no authenticated owner.");
}
