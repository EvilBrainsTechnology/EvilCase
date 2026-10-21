using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EarlyRefreshRestoreTests
{
    [Test]
    public async Task AnEarlyRefreshThatSucceededIsAppliedWithoutAnotherCall()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(10);
        var body = $$"""{"expiresAt":"{{expiresAt:O}}","email":"early@example.com","role":"Admin"}""";
        var (provider, tokens, authClient) = Setup(new EarlyRefreshResult(200, body));

        using (provider)
        {
            var state = await provider.GetAuthenticationStateAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(authClient.ReceivedCalls(), Is.Empty, "the refresh token rotates, so it is spent once");
                Assert.That(tokens.Current?.Email, Is.EqualTo("early@example.com"));
                Assert.That(state.User.Identity?.IsAuthenticated, Is.True);
            }
        }
    }

    [Test]
    public async Task AnEarlyRefreshTheServerRefusedSignsOutWithoutAnotherCall()
    {
        var (provider, tokens, authClient) = Setup(new EarlyRefreshResult(401, Body: null));

        using (provider)
        {
            var state = await provider.GetAuthenticationStateAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(authClient.ReceivedCalls(), Is.Empty, "a refused refresh token is not tried again");
                Assert.That(tokens.Current, Is.Null);
                Assert.That(state.User.Identity?.IsAuthenticated, Is.False);
            }
        }
    }

    [TestCase(null, null)]
    [TestCase(409, null)]
    [TestCase(429, null)]
    [TestCase(500, null)]
    [TestCase(200, "not json")]
    public async Task AMissingOrFailedEarlyRefreshFallsBackToRefresh(int? status, string? body)
    {
        var early = status is { } code ? new EarlyRefreshResult(code, body) : null;
        var (provider, tokens, authClient) = Setup(early);

        using (provider)
        {
            var state = await provider.GetAuthenticationStateAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(authClient.ReceivedCalls(), Has.Exactly(1).Items);
                Assert.That(tokens.Current?.Email, Is.EqualTo("late@example.com"));
                Assert.That(state.User.Identity?.IsAuthenticated, Is.True);
            }
        }
    }

    private static (EvilCaseAuthenticationStateProvider Provider, SessionStore Tokens, IAuthClient AuthClient) Setup(EarlyRefreshResult? early)
    {
        var authClient = Substitute.For<IAuthClient>();
        authClient
            .Refresh(Arg.Any<CancellationToken>())
            .Returns(new LoginResponse { ExpiresAt = DateTime.UtcNow.AddMinutes(10), Email = "late@example.com", Role = UserRole.User });

        var tokens = new SessionStore();
        var provider = new EvilCaseAuthenticationStateProvider(tokens, authClient, new StubEarlyRefresh(early), TimeProvider.System, NullLogger<EvilCaseAuthenticationStateProvider>.Instance);

        return (provider, tokens, authClient);
    }
}
