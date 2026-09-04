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
    // Mirrors RefreshToken.UserAgent's MaxLength.
    private const int UserAgentMaxLength = 256;

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<LoginResponse>> Login([FromServices] IAuthService authService, [FromBody] LoginRequest request, CancellationToken token)
    {
        var result = await authService.Login(request.Email, request.Password, this.DescribeClient(), token);

        if (result.Session is not { } session)
        {
            // A separate status for the lockout, so the page can say why without parsing a message.
            return result.Status == LoginStatus.LockedOut
                ? this.Problem(statusCode: StatusCodes.Status423Locked, title: "Account temporarily locked")
                : this.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials");
        }

        this.SetRefreshCookie(session);

        return this.Ok(Describe(session));
    }

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
            // Not on Raced: the cookie already holds another tab's replacement.
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
        return Guid.TryParse(this.User.FindFirstValue(AuthClaims.Subject), CultureInfo.InvariantCulture, out var userId)
            ? userId
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

    // Delete has to repeat these: a browser only drops a cookie whose attributes match.
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
