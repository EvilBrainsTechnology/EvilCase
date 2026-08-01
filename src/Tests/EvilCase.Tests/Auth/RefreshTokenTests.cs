using EvilBrains.EvilCase.Auth;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// Rotation and what it buys: a refresh token is good once, and a second use of a spent one is taken as
/// a stolen cookie and ends the session it came from.
/// </summary>
public class RefreshTokenTests
{
    /// <summary>
    /// Comfortably outside the window in which a replay is read as two tabs racing.
    /// </summary>
    private static readonly TimeSpan PastTheGracePeriod = TimeSpan.FromMinutes(5);

    private AuthTestHarness harness = null!;

    [SetUp]
    public void SetUp() => this.harness = new();

    [Test]
    public async Task RefreshingIssuesANewTokenAndSpendsTheOld()
    {
        var first = await this.harness.SignInAsync();

        this.harness.Time.Advance(TimeSpan.FromMinutes(20));

        var second = await this.harness.RefreshAsync(first.RefreshToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second, Is.Not.Null);
            Assert.That(second!.RefreshToken, Is.Not.EqualTo(first.RefreshToken));
            Assert.That(this.harness.RefreshTokens.All[0].RevokedAt, Is.Not.Null);
            Assert.That(this.harness.RefreshTokens.All[1].RevokedAt, Is.Null);
        }
    }

    /// <summary>
    /// One browser stays one session however often it renews, which is what makes the session list and
    /// signing a single device out mean anything.
    /// </summary>
    [Test]
    public async Task RotationStaysInsideTheSameSession()
    {
        var first = await this.harness.SignInAsync();

        _ = await this.harness.RefreshAsync(first.RefreshToken);

        Assert.That(this.harness.RefreshTokens.All.Select(token => token.SessionId).Distinct(), Has.Exactly(1).Items);
    }

    [Test]
    public async Task AReplayedTokenEndsTheWholeSession()
    {
        var first = await this.harness.SignInAsync();
        var second = await this.harness.RefreshAsync(first.RefreshToken);

        this.harness.Time.Advance(PastTheGracePeriod);

        var replayed = await this.harness.RefreshAsync(first.RefreshToken);
        var afterwards = await this.harness.RefreshAsync(second!.RefreshToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(replayed, Is.Null);

            // The one token that was still live goes with the session; whoever holds it holds a copy.
            Assert.That(afterwards, Is.Null);
            Assert.That(this.harness.RefreshTokens.All.Where(token => token.RevokedAt is null), Is.Empty);
        }
    }

    /// <summary>
    /// Two tabs presenting the same cookie at once is a race, not a theft. The loser is refused, but the
    /// session survives — the cookie already holds the replacement, so its next attempt succeeds. The
    /// status says so, because the endpoint above has to leave that cookie alone.
    /// </summary>
    [Test]
    public async Task AReplayInsideTheGraceWindowLeavesTheSessionAlone()
    {
        var first = await this.harness.SignInAsync();
        var second = await this.harness.RefreshAsync(first.RefreshToken);

        var raced = await this.harness.RefreshResultAsync(first.RefreshToken);
        var afterwards = await this.harness.RefreshAsync(second!.RefreshToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(raced.Session, Is.Null);
            Assert.That(raced.Status, Is.EqualTo(RefreshStatus.Raced));
            Assert.That(afterwards, Is.Not.Null);
        }
    }

    /// <summary>
    /// The read that finds a token live and the write that spends it are two statements, so two callers
    /// can both pass the first. The write is what settles it: the loser gets nothing, rather than a
    /// second live token in a chain that is supposed to hold exactly one.
    /// </summary>
    [Test]
    public async Task OnlyOneOfTwoCallersRacingForTheSameTokenSpendsIt()
    {
        var session = await this.harness.SignInAsync();

        this.harness.RefreshTokens.PauseBeforeRevoking();

        var first = this.harness.RefreshResultAsync(session.RefreshToken);
        var second = this.harness.RefreshResultAsync(session.RefreshToken);

        this.harness.RefreshTokens.Resume();

        var results = await Task.WhenAll(first, second);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Count(result => result.Session is not null), Is.EqualTo(1));
            Assert.That(results.Select(result => result.Status), Does.Contain(RefreshStatus.Raced));
            Assert.That(this.harness.RefreshTokens.All.Where(token => token.RevokedAt is null), Has.Exactly(1).Items);
        }
    }

    [Test]
    public async Task AnExpiredTokenIsRefused()
    {
        var session = await this.harness.SignInAsync();

        this.harness.Time.Advance(this.harness.Settings.RefreshToken.Expiration + TimeSpan.FromMinutes(1));

        Assert.That(await this.harness.RefreshAsync(session.RefreshToken), Is.Null);
    }

    /// <summary>
    /// Renewing must not be a way to stay signed in for ever, so the chain's ceiling caps every token
    /// issued inside it.
    /// </summary>
    [Test]
    public async Task RotationNeverReachesPastTheSessionCeiling()
    {
        var lifetime = this.harness.Settings.RefreshToken.Expiration;

        var session = await this.harness.SignInAsync();
        var ceiling = this.harness.RefreshTokens.All[0].SessionExpires;

        // Renewed just before each token would have run out, twice, which is how a browser that is used
        // every day walks a session towards its ceiling.
        this.harness.Time.Advance(lifetime - TimeSpan.FromDays(1));
        var renewed = await this.harness.RefreshAsync(session.RefreshToken);

        this.harness.Time.Advance(lifetime - TimeSpan.FromDays(1));
        var again = await this.harness.RefreshAsync(renewed!.RefreshToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(again, Is.Not.Null);
            Assert.That(again!.RefreshTokenExpires, Is.EqualTo(ceiling));
            Assert.That(this.harness.RefreshTokens.All[^1].SessionExpires, Is.EqualTo(ceiling));
        }
    }

    [Test]
    public async Task SigningOutEndsTheChainTheTokenBelongsTo()
    {
        var first = await this.harness.SignInAsync();
        var second = await this.harness.RefreshAsync(first.RefreshToken);

        await this.harness.Service.SignOutAsync(second!.RefreshToken, CancellationToken.None);

        Assert.That(await this.harness.RefreshAsync(second.RefreshToken), Is.Null);
    }

    [Test]
    public async Task SigningOutEverywhereEndsEverySession()
    {
        var phone = await this.harness.SignInAsync();
        var laptop = await this.harness.SignInAsync();

        await this.harness.Service.SignOutEverywhereAsync(this.harness.User.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.harness.RefreshAsync(phone.RefreshToken), Is.Null);
            Assert.That(await this.harness.RefreshAsync(laptop.RefreshToken), Is.Null);
        }
    }

    [Test]
    public async Task TheSessionListHasOneEntryPerBrowserRatherThanPerRenewal()
    {
        var phone = await this.harness.SignInAsync();
        _ = await this.harness.SignInAsync();
        _ = await this.harness.RefreshAsync(phone.RefreshToken);

        var sessions = await this.harness.Service.GetSessionsAsync(this.harness.User.Id, CancellationToken.None);

        Assert.That(sessions, Has.Exactly(2).Items);
    }

    [Test]
    public async Task ALockedOutAccountCannotRenew()
    {
        var session = await this.harness.SignInAsync();

        for (var attempt = 0; attempt < AuthTestHarness.MaxFailedAttempts; attempt++)
            _ = await this.harness.LoginAsync("not-the-password");

        Assert.That(await this.harness.RefreshAsync(session.RefreshToken), Is.Null);
    }

    [Test]
    public async Task AnUnknownTokenIsRefusedWithoutTouchingAnything()
    {
        _ = await this.harness.SignInAsync();

        var result = await this.harness.RefreshAsync("this-was-never-issued");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Null);
            Assert.That(this.harness.RefreshTokens.All[0].RevokedAt, Is.Null);
        }
    }
}
