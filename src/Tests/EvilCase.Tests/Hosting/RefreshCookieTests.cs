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
    public async Task SigningInReturnsNoTokenInTheBodyAndBothTokensInCookies()
    {
        using var response = await this.SignIn(Password);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        var cookie = RefreshCookieOf(response);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body?.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
            Assert.That(body?.Email, Is.EqualTo(Email));
            Assert.That(cookie, Does.Contain("httponly").IgnoreCase);
            Assert.That(cookie, Does.Contain("secure").IgnoreCase);
            Assert.That(cookie, Does.Contain("samesite=strict").IgnoreCase);
            Assert.That(cookie, Does.Contain("path=/"));
        }
    }

    [Test]
    public async Task SigningInSetsTheAccessCookieWithTheSameGuardsAsTheRefreshCookie()
    {
        using var response = await this.SignIn(Password);

        var cookie = AccessCookieOf(response);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ValueOf(cookie), Is.Not.Empty);
            Assert.That(cookie, Does.StartWith("__Host-"));
            Assert.That(cookie, Does.Contain("httponly").IgnoreCase);
            Assert.That(cookie, Does.Contain("secure").IgnoreCase);
            Assert.That(cookie, Does.Contain("samesite=strict").IgnoreCase);
            Assert.That(cookie, Does.Contain("path=/"));
            Assert.That(MaxAgeOf(cookie), Is.InRange(1, 15 * 60), "the cookie lapses with the token");
            Assert.That(cookie, Does.Not.Contain("expires=").IgnoreCase, "a browser clock that runs ahead would drop the cookie on arrival");
            Assert.That(cookie, Does.Not.Contain("domain=").IgnoreCase, "the host prefix forbids a domain");
        }
    }

    [Test]
    public async Task RenewingSetsTheAccessCookieAgain()
    {
        using var signIn = await this.SignIn(Password);

        using var refresh = await this.Refresh(ValueOf(RefreshCookieOf(signIn)));

        Assert.That(ValueOf(AccessCookieOf(refresh)), Is.Not.Empty);
    }

    [Test]
    public async Task TheApiAuthenticatesARequestThatCarriesOnlyTheAccessCookie()
    {
        using var signIn = await this.SignIn(Password);
        var token = ValueOf(AccessCookieOf(signIn));

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/auth/user-info", UriKind.Relative));
        request.Headers.Add("Cookie", $"{AccessCookie.Name}={token}");

        using var response = await this.client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<UserInfo>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body?.Email, Is.EqualTo(Email));
        }
    }

    [Test]
    public async Task AGarbledAccessCookieIsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/auth/user-info", UriKind.Relative));
        request.Headers.Add("Cookie", $"{AccessCookie.Name}=not-a-token");

        using var response = await this.client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
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
            Assert.That(ValueOf(AccessCookieOf(signOut)), Is.Empty, "the access cookie would keep the browser signed in");
            Assert.That(AccessCookieOf(signOut), Does.Contain("expires=Thu, 01 Jan 1970").IgnoreCase);
        }
    }

    [Test]
    public async Task SigningOutEverywhereWithOnlyTheAccessCookieClearsBothCookies()
    {
        using var signIn = await this.SignIn(Password);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(AuthRoute.Path + "/logout-all", UriKind.Relative));
        request.Headers.Add("Cookie", $"{AccessCookie.Name}={ValueOf(AccessCookieOf(signIn))}");

        using var signOut = await this.client.SendAsync(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signOut.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(ValueOf(RefreshCookieOf(signOut)), Is.Empty, "the refresh cookie would renew a revoked session");
            Assert.That(ValueOf(AccessCookieOf(signOut)), Is.Empty, "the access cookie would keep the browser signed in");
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
            Assert.That(ValueOf(AccessCookieOf(response)), Is.Empty, "a rejected refresh ends the whole session");
        }
    }

    private static string RefreshCookieOf(HttpResponseMessage response)
    {
        return CookieOf(response, RefreshCookie.Name);
    }

    private static string AccessCookieOf(HttpResponseMessage response)
    {
        return CookieOf(response, AccessCookie.Name);
    }

    private static string CookieOf(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.Single(value => value.StartsWith(name + "=", StringComparison.Ordinal))
            : throw new AssertionException("The response carries no Set-Cookie header");
    }

    private static int MaxAgeOf(string setCookie)
    {
        const string attribute = "max-age=";

        var start = setCookie.IndexOf(attribute, StringComparison.OrdinalIgnoreCase);

        if (start < 0)
            throw new AssertionException("The cookie carries no Max-Age");

        var digits = setCookie[(start + attribute.Length)..].TakeWhile(char.IsAsciiDigit).ToArray();

        return int.Parse(digits, CultureInfo.InvariantCulture);
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
