using System.Security.Claims;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route(AuthRoute.Template)]
public class AuthController : ControllerBase
{
    // The column behind it; a browser is free to send more than that.
    private const int UserAgentMaxLength = 256;

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, [FromServices] IAuthService authService, CancellationToken token)
    {
        var result = await authService.Login(request.Email, request.Password, this.DescribeClient(), token);

        if (result.Session is not { } session)
        {
            // A separate status for the lockout, so the page can say why without parsing a message. It
            // only ever reaches someone who already caused the failures, so it gives nothing away.
            return result.Status == LoginStatus.LockedOut
                ? this.Problem(statusCode: StatusCodes.Status423Locked, title: "Account temporarily locked")
                : this.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials");
        }

        this.SetRefreshCookie(session);

        return this.Ok(Describe(session));
    }

    /// <summary>
    /// Takes the refresh token from its cookie and nothing from the request body. Every call rotates:
    /// the token that came in is spent, and a replay of it later ends the whole session.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Refresh([FromServices] IAuthService authService, CancellationToken token)
    {
        var refreshToken = this.Request.Cookies[RefreshCookie.Name];

        var result = refreshToken is { Length: > 0 }
            ? await authService.Refresh(refreshToken, this.DescribeClient(), token)
            : RefreshResult.Failed(RefreshStatus.Rejected);

        if (result.Session is not { } session)
        {
            // Whatever it was, it is not usable; leaving it in place would only make the browser send it
            // again on every navigation. The one exception is a token another tab of this same browser
            // spent moments ago: the cookie now holds that tab's replacement, and deleting it here would
            // end the session that tab just renewed.
            if (result.Status != RefreshStatus.Raced)
                this.ClearRefreshCookie();

            return this.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid refresh token");
        }

        this.SetRefreshCookie(session);

        return this.Ok(Describe(session));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Logout([FromServices] IAuthService authService, CancellationToken token)
    {
        // Anonymous on purpose: an expired access token must not stop someone from signing out, and the
        // cookie is the only thing this needs.
        var refreshToken = this.Request.Cookies[RefreshCookie.Name];

        if (refreshToken is { Length: > 0 })
            await authService.SignOut(refreshToken, token);

        this.ClearRefreshCookie();

        return this.NoContent();
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> LogoutAll([FromServices] IAuthService authService, CancellationToken token)
    {
        await authService.SignOutEverywhere(this.UserId(), token);

        this.ClearRefreshCookie();

        return this.NoContent();
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<SessionInfo>>> Sessions([FromServices] IAuthService authService, CancellationToken token)
    {
        var current = this.CurrentAuthSessionId();
        var sessions = await authService.GetSessions(this.UserId(), token);

        return this.Ok(sessions.Select(session => Describe(session, current)).ToList());
    }

    [HttpGet("user-info")]
    [Authorize]
    public UserInfo UserInfo()
    {
        var email = this.User.Identity?.Name
            ?? throw new InvalidOperationException("Authenticated user is missing the name claim");

        return new() { Email = email, Role = this.Role() };
    }

    private static LoginResponse Describe(AuthSession session)
    {
        return new()
        {
            AccessToken = session.AccessToken,
            ExpiresAt = session.AccessTokenExpires,
            Email = session.Email,
            Role = session.Role,
        };
    }

    private static SessionInfo Describe(UserSession session, Guid? current)
    {
        return new()
        {
            AuthSessionId = session.AuthSessionId,
            Created = session.Created,
            Expires = session.Expires,
            LastUsed = session.LastUsed,
            IpAddress = session.IpAddress,
            UserAgent = session.UserAgent,
            IsCurrent = current == session.AuthSessionId,
        };
    }

    private Guid UserId()
    {
        return Guid.TryParse(this.User.FindFirstValue(AuthClaims.Subject), CultureInfo.InvariantCulture, out var id)
            ? id
            : throw new InvalidOperationException("Authenticated user is missing the subject claim");
    }

    private UserRole Role()
    {
        return Enum.TryParse<UserRole>(this.User.FindFirstValue(AuthClaims.Role), out var role) ? role : UserRole.User;
    }

    private Guid? CurrentAuthSessionId()
    {
        return Guid.TryParseExact(this.User.FindFirstValue(AuthClaims.AuthSessionId), "N", out var authSessionId) ? authSessionId : null;
    }

    private ClientInfo DescribeClient()
    {
        var userAgent = this.Request.Headers.UserAgent.ToString();

        return new()
        {
            IpAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = userAgent.Length switch
            {
                0 => null,
                <= UserAgentMaxLength => userAgent,
                _ => userAgent[..UserAgentMaxLength],
            },
        };
    }

    private void SetRefreshCookie(AuthSession session)
    {
        this.Response.Cookies.Append(RefreshCookie.Name, session.RefreshToken, CookieOptions(session.RefreshTokenExpires));
    }

    private void ClearRefreshCookie()
    {
        this.Response.Cookies.Delete(RefreshCookie.Name, CookieOptions(expires: null));
    }

    // Secure and the __Host- path are what the cookie's own name promises; HttpOnly keeps the token out
    // of reach of any script on the page, and Strict means no other site can make the browser send it.
    // Delete has to repeat all of it: a browser only drops a cookie whose attributes match.
    private static CookieOptions CookieOptions(DateTime? expires)
    {
        return new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookie.Path,
            Expires = expires,
            IsEssential = true,
        };
    }
}
