using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The refresh token only ever travels as a cookie, and the attributes on that cookie are the whole of
/// what keeps a script or another site from getting at it. They are set in one place and asserted here
/// against the raw header — the handler's own cookie container would drop a Secure cookie sent over the
/// plain-HTTP loopback the test server uses, and prove nothing either way.
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

        _ = users.Seed(new()
        {
            Email = Email,
            PasswordHash = PasswordHasher.Hash(Password),
            Role = UserRole.User,
            Created = DateTime.UtcNow,
        });

        // The two types that would reach for the database; everything else is the real host. The rate
        // limiter goes with them: this fixture signs in more often in a minute than a person ever would,
        // and what it is about is the cookie. RateLimitingTests is where the limits are pinned.
        this.host = new EvilCaseHost(configureServices: services =>
        {
            services.AddSingleton<IUserStore>(users);
            services.AddSingleton<IRefreshTokenStore>(new FakeRefreshTokenStore());

            services.Configure<RateLimiterOptions>(
                options => options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("tests")));
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
        using var response = await this.SignInAsync(Password);

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

    /// <summary>
    /// The <c>__Host-</c> prefix is what a browser enforces the rest against, so the name is not
    /// cosmetic.
    /// </summary>
    [Test]
    public async Task TheCookieCarriesTheHostPrefix()
    {
        using var response = await this.SignInAsync(Password);

        Assert.That(RefreshCookieOf(response), Does.StartWith("__Host-"));
    }

    [Test]
    public async Task WrongCredentialsAreUnauthorizedAndSetNoCookie()
    {
        using var response = await this.SignInAsync("not-the-password");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Headers.Contains("Set-Cookie"), Is.False);
        }
    }

    [Test]
    public async Task RenewingSwapsTheCookieForAnother()
    {
        using var signIn = await this.SignInAsync(Password);
        var first = ValueOf(RefreshCookieOf(signIn));

        using var refresh = await this.RefreshAsync(first);
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
        using var response = await this.PostAsync(AuthRoute.RefreshPath, cookie: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// A cookie the server no longer honours has to go, or the browser keeps sending it on every
    /// navigation for as long as it was good for.
    /// </summary>
    [Test]
    public async Task SigningOutClearsTheCookie()
    {
        using var signIn = await this.SignInAsync(Password);
        var issued = ValueOf(RefreshCookieOf(signIn));

        using var signOut = await this.PostAsync(AuthRoute.Path + "/logout", issued);

        var cleared = RefreshCookieOf(signOut);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signOut.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(ValueOf(cleared), Is.Empty);
            Assert.That(cleared, Does.Contain("expires=Thu, 01 Jan 1970").IgnoreCase);
        }
    }

    [Test]
    public async Task ASpentRefreshTokenIsRefusedAndTheCookieGoesWithIt()
    {
        using var signIn = await this.SignInAsync(Password);
        var first = ValueOf(RefreshCookieOf(signIn));

        using var renewed = await this.RefreshAsync(first);
        _ = renewed.StatusCode;

        using var replayed = await this.RefreshAsync(first);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(replayed.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(ValueOf(RefreshCookieOf(replayed)), Is.Empty);
        }
    }

    private static string RefreshCookieOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.Single(value => value.StartsWith(RefreshCookie.Name, StringComparison.Ordinal))
            : throw new AssertionException("The response carries no Set-Cookie header");

    private static string ValueOf(string setCookie) =>
        setCookie[(setCookie.IndexOf('=', StringComparison.Ordinal) + 1)..setCookie.IndexOf(';', StringComparison.Ordinal)];

    private Task<HttpResponseMessage> SignInAsync(string password) =>
        this.client.PostAsJsonAsync(
            new Uri(AuthRoute.LoginPath, UriKind.Relative),
            new LoginRequest { Email = Email, Password = password });

    private Task<HttpResponseMessage> RefreshAsync(string cookie) => this.PostAsync(AuthRoute.RefreshPath, cookie);

    private async Task<HttpResponseMessage> PostAsync(string path, string? cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative));

        if (cookie is not null)
            request.Headers.Add("Cookie", $"{RefreshCookie.Name}={cookie}");

        return await this.client.SendAsync(request);
    }
}
