using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.App.Auth;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SessionRenewalTests
{
    [Test]
    public async Task ARenewedSessionIsApplied()
    {
        var tokens = ExpiringSession.Store();

        using var provider = Provider(tokens, new StubEarlyRefresh(refreshed: StubEarlyRefresh.Renewed("renewed@example.com")));

        var renewed = await provider.Renew(tokens.Current, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renewed, Is.True);
            Assert.That(tokens.Current?.Email, Is.EqualTo("renewed@example.com"));
        }
    }

    [Test]
    public async Task AnUnfinishedRefreshLeavesTheSessionAlone()
    {
        var tokens = ExpiringSession.Store();

        using var provider = Provider(tokens, new StubEarlyRefresh(refreshed: null));

        var renewed = await provider.Renew(tokens.Current, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renewed, Is.False);
            Assert.That(tokens.Current, Is.Not.Null, "a failure the server never answered does not end the session");
        }
    }

    [TestCase(401)]
    [TestCase(409)]
    public async Task ARefusedRefreshEndsTheSession(int status)
    {
        var tokens = ExpiringSession.Store();

        using var provider = Provider(tokens, new StubEarlyRefresh(refreshed: new EarlyRefreshResult(status, Body: null)));

        var renewed = await provider.Renew(tokens.Current, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renewed, Is.False);
            Assert.That(tokens.Current, Is.Null, "a session the server refused is dropped");
        }
    }

    [TestCase(500, null)]
    [TestCase(200, "not json")]
    public async Task AFailingServerLeavesTheSessionAlone(int status, string? body)
    {
        var tokens = ExpiringSession.Store();

        using var provider = Provider(tokens, new StubEarlyRefresh(refreshed: new EarlyRefreshResult(status, body)));

        var renewed = await provider.Renew(tokens.Current, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renewed, Is.False);
            Assert.That(tokens.Current, Is.Not.Null, "only a refusal ends the session, not any other status");
        }
    }

    [Test]
    public async Task OnlyASessionAboutToLapseIsRenewedForABrowserRequest()
    {
        var refresh = new StubEarlyRefresh(refreshed: StubEarlyRefresh.Renewed("user@example.com"));
        var tokens = ExpiringSession.Store();

        using var provider = Provider(tokens, refresh);

        await provider.EnsureFresh(CancellationToken.None);
        await provider.EnsureFresh(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(refresh.Refreshes, Is.EqualTo(1), "a fresh session is not renewed again");
            Assert.That(tokens.Current?.ExpiresAt, Is.GreaterThan(DateTime.UtcNow.AddMinutes(5)));
        }
    }

    [Test]
    public async Task AKeptSessionIsRenewedAsItLapsesAndStopsWhenTheStreamCloses()
    {
        var refresh = new StubEarlyRefresh(refreshed: StubEarlyRefresh.Renewed("user@example.com"));
        var tokens = ExpiringSession.Store();

        using var provider = Provider(tokens, refresh);
        using var stream = new CancellationTokenSource();

        var kept = provider.KeepFresh(stream.Token);

        await stream.CancelAsync();
        await kept;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(refresh.Refreshes, Is.EqualTo(1));
            Assert.That(tokens.Current?.ExpiresAt, Is.GreaterThan(DateTime.UtcNow.AddMinutes(5)));
            Assert.That(kept.IsCompletedSuccessfully, Is.True, "a closed stream ends the renewal without an error");
        }
    }

    [Test]
    public async Task AKeptSessionTheServerRefusesStopsBeingRenewed()
    {
        var refresh = new StubEarlyRefresh(refreshed: new EarlyRefreshResult(401, Body: null));
        var tokens = ExpiringSession.Store();

        using var provider = Provider(tokens, refresh);

        await provider.KeepFresh(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(refresh.Refreshes, Is.EqualTo(1));
            Assert.That(tokens.Current, Is.Null);
        }
    }

    private static EvilCaseAuthenticationStateProvider Provider(ISessionStore tokens, StubEarlyRefresh refresh)
    {
        return new EvilCaseAuthenticationStateProvider(tokens, Substitute.For<IAuthClient>(), refresh, NullLogger<EvilCaseAuthenticationStateProvider>.Instance);
    }
}
