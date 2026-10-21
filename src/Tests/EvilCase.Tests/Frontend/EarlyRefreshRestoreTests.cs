using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.App.Auth;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EarlyRefreshRestoreTests
{
    [Test]
    public async Task AnEarlyRefreshThatSucceededIsAppliedWithoutAnotherCall()
    {
        var (provider, tokens, refresh) = Setup(StubEarlyRefresh.Renewed("early@example.com"));

        using (provider)
        {
            var state = await provider.GetAuthenticationStateAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refresh.Refreshes, Is.Zero, "the refresh token rotates, so it is spent once");
                Assert.That(tokens.Current?.Email, Is.EqualTo("early@example.com"));
                Assert.That(state.User.Identity?.IsAuthenticated, Is.True);
            }
        }
    }

    [TestCase(401)]
    [TestCase(409)]
    public async Task AnEarlyRefreshTheServerRefusedSignsOutWithoutAnotherCall(int status)
    {
        var (provider, tokens, refresh) = Setup(new EarlyRefreshResult(status, Body: null));

        using (provider)
        {
            var state = await provider.GetAuthenticationStateAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refresh.Refreshes, Is.Zero, "a refused refresh token is not tried again");
                Assert.That(tokens.Current, Is.Null);
                Assert.That(state.User.Identity?.IsAuthenticated, Is.False);
            }
        }
    }

    [TestCase(null, null)]
    [TestCase(429, null)]
    [TestCase(500, null)]
    [TestCase(200, "not json")]
    public async Task AMissingOrFailedEarlyRefreshFallsBackToRefresh(int? status, string? body)
    {
        var early = status is { } code ? new EarlyRefreshResult(code, body) : null;
        var (provider, tokens, refresh) = Setup(early);

        using (provider)
        {
            var state = await provider.GetAuthenticationStateAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refresh.Refreshes, Is.EqualTo(1));
                Assert.That(tokens.Current?.Email, Is.EqualTo("late@example.com"));
                Assert.That(state.User.Identity?.IsAuthenticated, Is.True);
            }
        }
    }

    private static (EvilCaseAuthenticationStateProvider Provider, SessionStore Tokens, StubEarlyRefresh Refresh) Setup(EarlyRefreshResult? early)
    {
        var refresh = new StubEarlyRefresh(early, StubEarlyRefresh.Renewed("late@example.com"));
        var tokens = new SessionStore();
        var provider = new EvilCaseAuthenticationStateProvider(tokens, Substitute.For<IAuthClient>(), refresh, NullLogger<EvilCaseAuthenticationStateProvider>.Instance);

        return (provider, tokens, refresh);
    }
}
