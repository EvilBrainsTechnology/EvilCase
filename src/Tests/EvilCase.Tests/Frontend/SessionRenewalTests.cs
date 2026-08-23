using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SessionRenewalTests
{
    [Test]
    public async Task AnUnreachableApiLeavesTheSessionAlone()
    {
        var tokens = Expiring();

        using var provider = Provider(tokens, new HttpRequestException("the network is down"));

        var renewed = await provider.Renew(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renewed, Is.False);
            Assert.That(tokens.Current, Is.Not.Null, "a failure the server never answered does not end the session");
        }
    }

    [Test]
    public async Task ARefusedRefreshTokenEndsTheSession()
    {
        var tokens = Expiring();

        using var provider = Provider(tokens, new ApiException(HttpStatusCode.Unauthorized, responseBody: null));

        var renewed = await provider.Renew(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renewed, Is.False);
            Assert.That(tokens.Current, Is.Null, "a session the server refused is dropped");
        }
    }

    [Test]
    public async Task AFailingServerLeavesTheSessionAlone()
    {
        var tokens = Expiring();

        using var provider = Provider(tokens, new ApiException(HttpStatusCode.InternalServerError, responseBody: null));

        var renewed = await provider.Renew(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renewed, Is.False);
            Assert.That(tokens.Current, Is.Not.Null, "only a refusal ends the session, not any other status");
        }
    }

    private static FakeAccessTokenStore Expiring()
    {
        var tokens = new FakeAccessTokenStore();

        // Inside the renew-ahead window, so the renewal actually reaches the client.
        tokens.Set(new()
        {
            Token = "access",
            ExpiresAt = DateTime.UtcNow,
            Email = "user@example.com",
            Role = UserRole.User,
        });

        return tokens;
    }

    private static EvilCaseAuthenticationStateProvider Provider(IAccessTokenStore tokens, Exception refreshFailure)
    {
        return new EvilCaseAuthenticationStateProvider(
            tokens,
            new FakeAuthClient(refreshFailure),
            NullLogger<EvilCaseAuthenticationStateProvider>.Instance);
    }
}
