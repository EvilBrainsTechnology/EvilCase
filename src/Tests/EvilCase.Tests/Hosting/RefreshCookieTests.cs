using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// Asserted against the raw <c>Set-Cookie</c> header: the handler's cookie container would drop a Secure
/// cookie over the test server's plain-HTTP loopback.
/// </summary>
public class RefreshCookieTests
{
    private const string Email = "user@evilcase.test";

    private const string Password = "correct-horse-battery-staple";

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        var users = new FakeUserStore();

        users.SeedUser(new()
        {
            TenantId = Guid.CreateVersion7(),
            Email = Email,
            PasswordHash = PasswordHasher.Hash(Password),
            Role = UserRole.User,
        });

        // The limiter is off: this fixture signs in more often than the login partition allows;
        // RateLimitingTests pins the limits.
        this.host = new EvilCaseHost(configureServices: services =>
        {
            services.AddSingleton<IUserStore>(users);
            services.AddSingleton<IRefreshTokenStore>(new FakeRefreshTokenStore(TimeProvider.System));

            services.Configure<RateLimiterOptions>(
                static options => options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    static _ => RateLimitPartition.GetNoLimiter("tests")));
        });

        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [Test]
    public async Task SigningInReturnsAnAccessTokenInTheBodyAndTheRefreshTokenOnlyInTheCookie()
    {
        using var response = await this.SignIn(Password);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        var cookie = RefreshCookieOf(response);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body?.AccessToken, Is.Not.Empty);
            Assert.That(body?.Email, Is.EqualTo(Email));
            Assert.That(cookie, Does.Contain("httponly").IgnoreCase);
            Assert.That(cookie, Does.Contain("secure").IgnoreCase);
            Assert.That(cookie, Does.Contain("samesite=strict").IgnoreCase);
            Assert.That(cookie, Does.Contain("path=/"));
        }
    }

    [Test]
    public async Task TheCookieCarriesTheHostPrefix()
    {
        using var response = await this.SignIn(Password);

        Assert.That(RefreshCookieOf(response), Does.StartWith("__Host-"), "the prefix is what a browser enforces the other attributes against");
    }

    [Test]
    public async Task WrongCredentialsAreUnauthorizedAndSetNoCookie()
    {
        using var response = await this.SignIn("not-the-password");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Headers.Contains("Set-Cookie"), Is.False);
        }
    }

    [Test]
    public async Task RenewingSwapsTheCookieForAnother()
    {
        using var signIn = await this.SignIn(Password);
        var first = ValueOf(RefreshCookieOf(signIn));

        using var refresh = await this.Refresh(first);
        var second = ValueOf(RefreshCookieOf(refresh));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(refresh.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(second, Is.Not.Empty);
        }
    }

    [Test]
    public async Task RenewingWithoutACookieIsRefused()
    {
        using var response = await this.Post(AuthRoute.RefreshPath, cookie: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task SigningOutClearsTheCookie()
    {
        using var signIn = await this.SignIn(Password);
        var issued = ValueOf(RefreshCookieOf(signIn));

        using var signOut = await this.Post(AuthRoute.LogoutPath, issued);

        var cleared = RefreshCookieOf(signOut);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signOut.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(ValueOf(cleared), Is.Empty, "a cookie the server no longer honours is sent on every navigation until it is cleared");
            Assert.That(cleared, Does.Contain("expires=Thu, 01 Jan 1970").IgnoreCase, "a cookie the server no longer honours is sent on every navigation until it is cleared");
        }
    }

    [Test]
    public async Task ASpentRefreshTokenIsRefusedButLeavesTheReplacementCookieAlone()
    {
        using var signIn = await this.SignIn(Password);
        var first = ValueOf(RefreshCookieOf(signIn));

        using var renewed = await this.Refresh(first);
        Assert.That(renewed.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the tab that renewed first must be the one holding the live token");

        using var replayed = await this.Refresh(first);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(replayed.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(replayed.Headers.Contains("Set-Cookie"), Is.False, "a delete matches by name alone, so it would take the replacement the cookie already holds");
        }
    }

    [Test]
    public async Task AnUnknownRefreshTokenIsRefusedAndTheCookieGoesWithIt()
    {
        using var response = await this.Refresh("this-was-never-issued");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(ValueOf(RefreshCookieOf(response)), Is.Empty, "a token that was never issued is no race, so the cookie goes with it");
        }
    }

    private static string RefreshCookieOf(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.Single(static value => value.StartsWith(RefreshCookie.Name, StringComparison.Ordinal))
            : throw new AssertionException("The response carries no Set-Cookie header");
    }

    private static string ValueOf(string setCookie)
    {
        return setCookie[(setCookie.IndexOf('=', StringComparison.Ordinal) + 1)..setCookie.IndexOf(';', StringComparison.Ordinal)];
    }

    private async Task<HttpResponseMessage> SignIn(string password)
    {
        return await this.client.PostAsJsonAsync(
            new Uri(AuthRoute.LoginPath, UriKind.Relative),
            new LoginRequest { Email = Email, Password = password });
    }

    private async Task<HttpResponseMessage> Refresh(string cookie)
    {
        return await this.Post(AuthRoute.RefreshPath, cookie);
    }

    private async Task<HttpResponseMessage> Post(string path, string? cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative));

        if (cookie is not null)
            request.Headers.Add("Cookie", $"{RefreshCookie.Name}={cookie}");

        return await this.client.SendAsync(request);
    }
}
