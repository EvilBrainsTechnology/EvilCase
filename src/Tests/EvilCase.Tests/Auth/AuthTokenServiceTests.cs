using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// The access token is the only thing an authenticated request carries, so everything a controller can
/// ask about the caller has to be in it.
/// </summary>
public class AuthTokenServiceTests
{
    private static readonly Guid SessionId = Guid.Parse("0f9b8a7c-6d5e-4f3a-2b1c-0d9e8f7a6b5c", CultureInfo.InvariantCulture);

    [Test]
    public void TheTokenNamesTheUserTheirRoleAndTheirSession()
    {
        var harness = new AuthTestHarness();
        var settings = harness.Settings;

        var service = new AuthTokenService(Options.Create(settings), harness.Time);

        var token = new JsonWebTokenHandler().ReadJsonWebToken(service.Generate(harness.User, SessionId).Value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(token.GetClaim(AuthClaims.Subject).Value, Is.EqualTo(harness.User.Id.ToString(CultureInfo.InvariantCulture)));
            Assert.That(token.GetClaim(AuthClaims.Email).Value, Is.EqualTo(harness.User.Email));
            Assert.That(token.GetClaim(AuthClaims.Role).Value, Is.EqualTo(nameof(UserRole.Admin)));
            Assert.That(token.GetClaim(AuthClaims.SessionId).Value, Is.EqualTo(SessionId.ToString("N", CultureInfo.InvariantCulture)));
            Assert.That(token.Issuer, Is.EqualTo(settings.Jwt.Issuer));
            Assert.That(token.Audiences, Does.Contain(settings.Jwt.Audience));
        }
    }

    /// <summary>
    /// Two tokens for the same user must not be interchangeable, so each carries its own identifier.
    /// </summary>
    [Test]
    public void EveryTokenGetsItsOwnIdentifier()
    {
        var harness = new AuthTestHarness();
        var service = new AuthTokenService(Options.Create(harness.Settings), harness.Time);

        var first = new JsonWebTokenHandler().ReadJsonWebToken(service.Generate(harness.User, SessionId).Value);
        var second = new JsonWebTokenHandler().ReadJsonWebToken(service.Generate(harness.User, SessionId).Value);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public void TheReportedExpiryIsTheOneInTheToken()
    {
        var harness = new AuthTestHarness();
        var service = new AuthTokenService(Options.Create(harness.Settings), harness.Time);

        var generated = service.Generate(harness.User, SessionId);
        var token = new JsonWebTokenHandler().ReadJsonWebToken(generated.Value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(generated.ExpiresAt, Is.EqualTo(harness.Time.UtcNow.Add(harness.Settings.Jwt.AccessTokenExpiration)));
            Assert.That(token.ValidTo, Is.EqualTo(generated.ExpiresAt).Within(TimeSpan.FromSeconds(1)));
        }
    }
}
