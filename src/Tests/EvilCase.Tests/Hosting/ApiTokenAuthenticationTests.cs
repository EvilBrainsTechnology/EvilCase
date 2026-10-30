using System.Net;
using System.Net.Http.Headers;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Labels;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.ApiTokens;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The store is a fake; the scheme selection, the claims, the boundary and the authorization run as in production.
/// </summary>
public class ApiTokenAuthenticationTests
{
    private const string CasesPath = "/api/cases?take=10";

    private readonly FakeApiTokenStore store = new();

    private readonly List<Guid> tenantsSeen = [];

    private readonly User user = new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        Email = "agent@evilcase.test",
        PasswordHash = "not-verified-here",
        Role = UserRole.User,
    };

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        var labels = Substitute.For<ILabelWriter>();
        labels
            .DeleteLabel(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(DeleteOutcome.Deleted);

        this.host = new EvilCaseHost(configureServices: services =>
        {
            services.AddSingleton<IApiTokenStore>(this.store);
            services.AddSingleton(labels);

            // Records the tenant the request runs under when the reader is asked.
            services.AddScoped(provider =>
            {
                var userContext = provider.GetRequiredService<IUserContext>();
                var reader = Substitute.For<ICaseReader>();

                reader
                    .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
                    .Returns(_ =>
                    {
                        this.tenantsSeen.Add(userContext.TenantId);

                        return new CaseListResponse { Items = [], TotalCount = 0 };
                    });

                return reader;
            });
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
    public async Task ARequestRunsUnderTheTenantOfTheTokensUser()
    {
        var secret = this.store.Issue(this.user, ApiTokenScope.ReadOnly);
        this.tenantsSeen.Clear();

        using var response = await this.Send(HttpMethod.Get, CasesPath, secret);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(this.tenantsSeen, Is.EqualTo([this.user.TenantId]), "a token acts as its user, tenant included");
        }
    }

    [Test]
    public async Task AReadOnlyTokenReadsButNeverWrites()
    {
        var secret = this.store.Issue(this.user, ApiTokenScope.ReadOnly);

        using var read = await this.Send(HttpMethod.Get, CasesPath, secret);
        using var write = await this.Send(HttpMethod.Delete, $"/api/labels/{Guid.CreateVersion7()}", secret);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(write.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), "a read-only token gets 403 on every write");
        }
    }

    [Test]
    public async Task AReadWriteTokenWrites()
    {
        var secret = this.store.Issue(this.user, ApiTokenScope.ReadWrite);

        using var write = await this.Send(HttpMethod.Delete, $"/api/labels/{Guid.CreateVersion7()}", secret);

        Assert.That(write.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task AnExpiredTokenIsUnauthorized()
    {
        var secret = this.store.Issue(this.user, ApiTokenScope.ReadWrite, expires: DateTime.UtcNow.AddSeconds(-1));

        using var response = await this.Send(HttpMethod.Get, CasesPath, secret);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AnUnknownTokenIsUnauthorized()
    {
        using var response = await this.Send(HttpMethod.Get, CasesPath, ApiTokenValue.Create());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ARevokedTokenStopsOnTheNextRequest()
    {
        var secret = this.store.Issue(this.user, ApiTokenScope.ReadOnly);

        using var before = await this.Send(HttpMethod.Get, CasesPath, secret);
        this.store.Forget(secret);
        using var after = await this.Send(HttpMethod.Get, CasesPath, secret);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(before.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(after.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "nothing caches a token, so the revocation holds on the very next request");
        }
    }

    [Test]
    public async Task ALockedOutUserKeepsTheirToken()
    {
        var locked = this.user with { Id = Guid.CreateVersion7(), Email = "locked@evilcase.test", LockoutEnd = DateTime.UtcNow.AddMinutes(15) };
        var secret = this.store.Issue(locked, ApiTokenScope.ReadOnly);

        using var response = await this.Send(HttpMethod.Get, CasesPath, secret);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the lockout blocks the password sign-in alone");
    }

    [TestCase("GET", "/api/api-tokens")]
    [TestCase("POST", "/api/api-tokens")]
    [TestCase("DELETE", "/api/api-tokens/0199a1b2-0000-7000-8000-000000000000")]
    [TestCase("GET", "/api/auth/user-info")]
    [TestCase("GET", "/api/auth/sessions")]
    [TestCase("POST", "/api/auth/logout-all")]
    [TestCase("POST", "/api/auth/logout")]
    public async Task ATokenCannotReachSignInOrTokenManagement(string method, string path)
    {
        var secret = this.store.Issue(this.user, ApiTokenScope.ReadWrite);

        using var response = await this.Send(new HttpMethod(method), path, secret);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), "token management and /api/auth stay cookie sessions only");
    }

    [Test]
    public async Task ATokenBesideTheCookieAuthenticatesTheRequest()
    {
        var secret = this.store.Issue(this.user, ApiTokenScope.ReadWrite);
        var cookie = TestTokens.TokenFrom(this.host);

        using var cookieOnly = await this.Send(HttpMethod.Get, "/api/api-tokens", secret: null, cookie);
        using var both = await this.Send(HttpMethod.Get, "/api/api-tokens", secret, cookie);
        using var unknownBeside = await this.Send(HttpMethod.Get, "/api/api-tokens", ApiTokenValue.Create(), cookie);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cookieOnly.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(both.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), "the token wins over the cookie, and a token never manages tokens");
            Assert.That(unknownBeside.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "a refused token never falls back to the cookie");
        }
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? secret, string? cookie = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));

        if (secret is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);

        if (cookie is not null)
            request.Headers.Add("Cookie", $"{AccessCookie.Name}={cookie}");

        return await this.client.SendAsync(request);
    }
}
