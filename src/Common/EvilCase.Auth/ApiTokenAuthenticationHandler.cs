using System.Security.Claims;
using System.Text.Encodings.Web;
using EvilBrains.EvilCase.Api.Contract.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// Reads the token and its user from the database on every request, so a revocation holds from the
/// next request and tenant and role follow the user row. A lockout does not stop it.
/// </summary>
internal sealed class ApiTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IApiTokenStore apiTokenStore,
    TimeProvider timeProvider) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "ApiToken";

    private static readonly TimeSpan LastUsedResolution = TimeSpan.FromMinutes(1);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (ApiTokenValue.Read(this.Request) is not { } presented)
            return AuthenticateResult.NoResult();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var stored = await apiTokenStore.FindApiToken(ApiTokenValue.Hash(presented), this.Context.RequestAborted);

        if (stored?.User is not { } user || stored.Expires <= now)
            return AuthenticateResult.Fail("The API token is unknown, revoked or expired.");

        await apiTokenStore.TouchApiToken(stored.Id, now, now - LastUsedResolution, this.Context.RequestAborted);

        Claim[] claims =
        [
            new(AuthClaims.Subject, user.Id.ToString("D", CultureInfo.InvariantCulture)),
            new(AuthClaims.Email, user.Email),
            new(AuthClaims.Role, user.Role.ToString()),
            new(AuthClaims.Tenant, user.TenantId.ToString("D", CultureInfo.InvariantCulture)),
            new(AuthClaims.ApiTokenId, stored.Id.ToString("D", CultureInfo.InvariantCulture)),
            new(AuthClaims.ApiTokenName, stored.Name),
            new(AuthClaims.ApiTokenScope, stored.Scope.ToString()),
        ];

        var identity = new ClaimsIdentity(claims, this.Scheme.Name, AuthClaims.Email, AuthClaims.Role);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name));
    }
}
