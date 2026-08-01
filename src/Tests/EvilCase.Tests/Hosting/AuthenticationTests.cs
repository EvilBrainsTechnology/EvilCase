using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The only authenticated endpoint the application has. Tokens are minted through the service the login
/// endpoint uses, so a signing configuration that drifts apart from the one the bearer scheme validates
/// against fails here rather than only in a browser. Nothing below reaches the database: the endpoint
/// answers from the claims principal alone.
/// </summary>
public class AuthenticationTests
{
    private const string UserInfoPath = "/api/auth/user-info";

    private const string Email = "user@evilcase.test";

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost();
        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [Test]
    public async Task UserInfoRejectsACallerWithoutAToken()
    {
        using var response = await this.GetUserInfoAsync(token: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task UserInfoAnswersTheCallerNamedByTheToken()
    {
        using var response = await this.GetUserInfoAsync(TokenFrom(this.host));

        var body = await response.Content.ReadFromJsonAsync<UserInfo>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body?.Email, Is.EqualTo(Email));
        }
    }

    /// <summary>
    /// Same issuer, audience and algorithm, only the key differs — so this passes everything the bearer
    /// scheme checks except the signature.
    /// </summary>
    [Test]
    public async Task UserInfoRejectsATokenSignedWithAnotherKey()
    {
        await using var foreignHost = new EvilCaseHost(jwtKey: new string('x', 64));

        using var response = await this.GetUserInfoAsync(TokenFrom(foreignHost));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static string TokenFrom(EvilCaseHost host)
    {
        using var scope = host.Services.CreateScope();

        var user = new User { Email = Email, PasswordHash = "not-verified-here", Created = DateTime.UtcNow };

        return scope.ServiceProvider.GetRequiredService<IAuthTokenService>().GenerateToken(user);
    }

    private async Task<HttpResponseMessage> GetUserInfoAsync(string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(UserInfoPath, UriKind.Relative));

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await this.client.SendAsync(request);
    }
}
