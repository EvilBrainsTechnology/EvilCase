using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.App.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SessionRenewalTests
{
    [Test]
    public async Task AnUnreachableApiLeavesTheSessionAlone()
    {
        var tokens = ExpiringSession.Store();

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
        var tokens = ExpiringSession.Store();

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
        var tokens = ExpiringSession.Store();

        using var provider = Provider(tokens, new ApiException(HttpStatusCode.InternalServerError, responseBody: null));

        var renewed = await provider.Renew(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renewed, Is.False);
            Assert.That(tokens.Current, Is.Not.Null, "only a refusal ends the session, not any other status");
        }
    }

    private static EvilCaseAuthenticationStateProvider Provider(IAccessTokenStore tokens, Exception refreshFailure)
    {
        var authClient = Substitute.For<IAuthClient>();
        authClient
            .Refresh(Arg.Any<CancellationToken>())
            .ThrowsAsync(refreshFailure);

        return new EvilCaseAuthenticationStateProvider(tokens, authClient, NullLogger<EvilCaseAuthenticationStateProvider>.Instance);
    }
}
