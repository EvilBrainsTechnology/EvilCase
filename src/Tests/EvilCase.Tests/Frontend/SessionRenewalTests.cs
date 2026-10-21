using System.Net;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.Domain.Users;
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

        var renewed = await provider.Renew(tokens.Current, CancellationToken.None);

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

        var renewed = await provider.Renew(tokens.Current, CancellationToken.None);

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

        var renewed = await provider.Renew(tokens.Current, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renewed, Is.False);
            Assert.That(tokens.Current, Is.Not.Null, "only a refusal ends the session, not any other status");
        }
    }

    [Test]
    public async Task ASupersededRefreshIsRetriedUntilItSucceeds()
    {
        var authClient = Substitute.For<IAuthClient>();
        authClient
            .Refresh(Arg.Any<CancellationToken>())
            .Returns(
                Superseded(),
                Superseded(),
                Task.FromResult(new LoginResponse { ExpiresAt = DateTime.UtcNow.AddMinutes(10), Email = "renewed@example.com", Role = UserRole.User }));

        var tokens = ExpiringSession.Store();
        var time = new InstantTimeProvider();

        using var provider = new EvilCaseAuthenticationStateProvider(tokens, authClient, new StubEarlyRefresh(), time, NullLogger<EvilCaseAuthenticationStateProvider>.Instance);

        var renewed = await provider.Renew(tokens.Current, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renewed, Is.True);
            Assert.That(tokens.Current?.Email, Is.EqualTo("renewed@example.com"));
            Assert.That(authClient.ReceivedCalls(), Has.Exactly(3).Items);
            Assert.That(time.Delays, Is.EqualTo([TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(500)]), "a retry waits for the browser to store the other tab's cookie");
        }
    }

    [Test]
    public async Task ARefreshStillSupersededAfterTheRetriesLeavesTheSessionAlone()
    {
        var authClient = Substitute.For<IAuthClient>();
        authClient
            .Refresh(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException(HttpStatusCode.Conflict, responseBody: null));

        var tokens = ExpiringSession.Store();

        using var provider = new EvilCaseAuthenticationStateProvider(tokens, authClient, new StubEarlyRefresh(), new InstantTimeProvider(), NullLogger<EvilCaseAuthenticationStateProvider>.Instance);

        var renewed = await provider.Renew(tokens.Current, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renewed, Is.False);
            Assert.That(tokens.Current, Is.Not.Null, "another tab holds the session, so a superseded refresh never ends it");
            Assert.That(authClient.ReceivedCalls(), Has.Exactly(4).Items, "the retries are bounded");
        }
    }

    [Test]
    public async Task OnlyASessionAboutToLapseIsRenewedForABrowserRequest()
    {
        var authClient = Substitute.For<IAuthClient>();
        authClient
            .Refresh(Arg.Any<CancellationToken>())
            .Returns(new LoginResponse { ExpiresAt = DateTime.UtcNow.AddMinutes(10), Email = "user@example.com", Role = UserRole.User });

        var tokens = ExpiringSession.Store();

        using var provider = new EvilCaseAuthenticationStateProvider(tokens, authClient, new StubEarlyRefresh(), TimeProvider.System, NullLogger<EvilCaseAuthenticationStateProvider>.Instance);

        await provider.EnsureFresh(CancellationToken.None);
        await provider.EnsureFresh(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(authClient.ReceivedCalls(), Has.Exactly(1).Items, "a fresh session is not renewed again");
            Assert.That(tokens.Current?.ExpiresAt, Is.GreaterThan(DateTime.UtcNow.AddMinutes(5)));
        }
    }

    private static EvilCaseAuthenticationStateProvider Provider(ISessionStore tokens, Exception refreshFailure)
    {
        var authClient = Substitute.For<IAuthClient>();
        authClient
            .Refresh(Arg.Any<CancellationToken>())
            .ThrowsAsync(refreshFailure);

        return new EvilCaseAuthenticationStateProvider(tokens, authClient, new StubEarlyRefresh(), TimeProvider.System, NullLogger<EvilCaseAuthenticationStateProvider>.Instance);
    }

    private static Task<LoginResponse> Superseded()
    {
        return Task.FromException<LoginResponse>(new ApiException(HttpStatusCode.Conflict, responseBody: null));
    }
}
