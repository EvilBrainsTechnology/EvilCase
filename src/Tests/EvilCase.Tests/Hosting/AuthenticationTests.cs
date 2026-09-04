using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// Inbound claim mapping is off, so a renamed claim silently empties the principal; the endpoint
/// answers from the principal alone.
/// </summary>
public class AuthenticationTests
{
    private const string UserInfoPath = "/api/auth/user-info";

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
        using var response = await this.GetUserInfo(token: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task UserInfoAnswersTheCallerNamedByTheToken()
    {
        using var response = await this.GetUserInfo(TestTokens.TokenFrom(this.host));

        var body = await response.Content.ReadFromJsonAsync<UserInfo>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body?.Email, Is.EqualTo(TestTokens.Email));
            Assert.That(body?.Role, Is.EqualTo(UserRole.Admin));
        }
    }

    [Test]
    public async Task UserInfoRejectsATokenSignedWithAnotherKey()
    {
        await using var foreignHost = new EvilCaseHost(jwtKey: new string('x', 64));

        using var response = await this.GetUserInfo(TestTokens.TokenFrom(foreignHost));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "the token shares issuer, audience and algorithm, so only the signature can refuse it");
    }

    private async Task<HttpResponseMessage> GetUserInfo(string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(UserInfoPath, UriKind.Relative));

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await this.client.SendAsync(request);
    }
}
