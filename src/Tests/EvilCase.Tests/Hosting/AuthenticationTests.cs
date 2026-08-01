using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EvilBrains.EvilCase.Api.Contract.User;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// What the bearer scheme makes of a token the application itself signed. Nothing below reaches the
/// database: the endpoint answers from the claims principal alone, which is also what pins the claim
/// names — inbound mapping is off, so a rename would silently empty the principal.
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

    /// <summary>
    /// Same issuer, audience and algorithm, only the key differs — so this passes everything the bearer
    /// scheme checks except the signature.
    /// </summary>
    [Test]
    public async Task UserInfoRejectsATokenSignedWithAnotherKey()
    {
        await using var foreignHost = new EvilCaseHost(jwtKey: new string('x', 64));

        using var response = await this.GetUserInfo(TestTokens.TokenFrom(foreignHost));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private async Task<HttpResponseMessage> GetUserInfo(string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(UserInfoPath, UriKind.Relative));

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await this.client.SendAsync(request);
    }
}
