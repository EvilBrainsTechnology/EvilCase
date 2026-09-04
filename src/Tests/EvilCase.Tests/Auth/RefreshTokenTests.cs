using EvilBrains.EvilCase.Auth;

namespace EvilBrains.EvilCase.Tests.Auth;

public class RefreshTokenTests
{
    private static readonly TimeSpan PastTheGracePeriod = TimeSpan.FromMinutes(5);

    private AuthTestHarness harness = null!;

    [SetUp]
    public void SetUp()
    {
        this.harness = new();
    }

    [Test]
    public async Task RefreshingIssuesANewTokenAndSpendsTheOld()
    {
        var first = await this.harness.SignIn();

        this.harness.Time.Advance(TimeSpan.FromMinutes(20));

        var second = await this.harness.Refresh(first.RefreshToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second, Is.Not.Null);
            Assert.That(second!.RefreshToken, Is.Not.EqualTo(first.RefreshToken));
            Assert.That(this.harness.RefreshTokens.All[0].RevokedAt, Is.Not.Null);
            Assert.That(this.harness.RefreshTokens.All[1].RevokedAt, Is.Null);
        }
    }

    [Test]
    public async Task RotationStaysInsideTheSameSession()
    {
        var first = await this.harness.SignIn();

        await this.harness.Refresh(first.RefreshToken);

        Assert.That(this.harness.RefreshTokens.All.Select(static token => token.AuthSessionId).Distinct(), Has.Exactly(1).Items);
    }

    [Test]
    public async Task AReplayedTokenEndsTheWholeSession()
    {
        var first = await this.harness.SignIn();
        var second = await this.harness.Refresh(first.RefreshToken);

        this.harness.Time.Advance(PastTheGracePeriod);

        var replayed = await this.harness.Refresh(first.RefreshToken);
        var afterwards = await this.harness.Refresh(second!.RefreshToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(replayed, Is.Null);
            Assert.That(afterwards, Is.Null);
            Assert.That(this.harness.RefreshTokens.All.Where(static token => token.RevokedAt is null), Is.Empty);
        }
    }

    // The Raced status exists so the endpoint leaves the cookie alone: it already holds the replacement.
    [Test]
    public async Task AReplayInsideTheGraceWindowLeavesTheSessionAlone()
    {
        var first = await this.harness.SignIn();
        var second = await this.harness.Refresh(first.RefreshToken);

        var raced = await this.harness.RefreshOutcome(first.RefreshToken);
        var afterwards = await this.harness.Refresh(second!.RefreshToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(raced.Session, Is.Null);
            Assert.That(raced.Status, Is.EqualTo(RefreshStatus.Raced));
            Assert.That(afterwards, Is.Not.Null);
        }
    }

    // The read that finds a token live and the write that spends it are two statements, so the write must settle the race.
    [Test]
    public async Task OnlyOneOfTwoCallersRacingForTheSameTokenSpendsIt()
    {
        var session = await this.harness.SignIn();

        this.harness.RefreshTokens.PauseBeforeRevoking();

        var first = this.harness.RefreshOutcome(session.RefreshToken);
        var second = this.harness.RefreshOutcome(session.RefreshToken);

        this.harness.RefreshTokens.Resume();

        var results = await Task.WhenAll(first, second);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Count(static result => result.Session is not null), Is.EqualTo(1));
            Assert.That(results.Select(static result => result.Status), Does.Contain(RefreshStatus.Raced));
            Assert.That(this.harness.RefreshTokens.All.Where(static token => token.RevokedAt is null), Has.Exactly(1).Items);
        }
    }

    [Test]
    public async Task AnExpiredTokenIsRefused()
    {
        var session = await this.harness.SignIn();

        this.harness.Time.Advance(this.harness.Settings.RefreshToken.Expiration + TimeSpan.FromMinutes(1));

        Assert.That(await this.harness.Refresh(session.RefreshToken), Is.Null);
    }

    [Test]
    public async Task RotationNeverReachesPastTheSessionCeiling()
    {
        var lifetime = this.harness.Settings.RefreshToken.Expiration;

        var session = await this.harness.SignIn();
        var ceiling = this.harness.RefreshTokens.All[0].SessionExpires;

        this.harness.Time.Advance(lifetime - TimeSpan.FromDays(1));
        var renewed = await this.harness.Refresh(session.RefreshToken);

        this.harness.Time.Advance(lifetime - TimeSpan.FromDays(1));
        var again = await this.harness.Refresh(renewed!.RefreshToken);

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
        var first = await this.harness.SignIn();
        var second = await this.harness.Refresh(first.RefreshToken);

        await this.harness.Service.SignOut(second!.RefreshToken, CancellationToken.None);

        Assert.That(await this.harness.Refresh(second.RefreshToken), Is.Null);
    }

    [Test]
    public async Task SigningOutEverywhereEndsEverySession()
    {
        var phone = await this.harness.SignIn();
        var laptop = await this.harness.SignIn();

        await this.harness.Service.SignOutEverywhere(this.harness.User.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.harness.Refresh(phone.RefreshToken), Is.Null);
            Assert.That(await this.harness.Refresh(laptop.RefreshToken), Is.Null);
        }
    }

    [Test]
    public async Task TheSessionListHasOneEntryPerBrowserRatherThanPerRenewal()
    {
        var phone = await this.harness.SignIn();
        await this.harness.SignIn();
        await this.harness.Refresh(phone.RefreshToken);

        var sessions = await this.harness.Service.GetSessions(this.harness.User.Id, CancellationToken.None);

        Assert.That(sessions, Has.Exactly(2).Items);
    }

    // Rotation replaces the row, so the live token knows only its last renewal; sign-in comes from the chain's first row.
    [Test]
    public async Task ASessionIsDatedFromTheSignInAndFromItsLastRenewal()
    {
        var signedInAt = this.harness.Time.UtcNow;
        var session = await this.harness.SignIn();

        this.harness.Time.Advance(TimeSpan.FromDays(3));
        var renewedAt = this.harness.Time.UtcNow;
        await this.harness.Refresh(session.RefreshToken);

        var listed = (await this.harness.Service.GetSessions(this.harness.User.Id, CancellationToken.None)).Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(listed.Created, Is.EqualTo(signedInAt));
            Assert.That(listed.LastUsed, Is.EqualTo(renewedAt));
        }
    }

    [Test]
    public async Task ALockedOutAccountCannotRenew()
    {
        var session = await this.harness.SignIn();

        for (var attempt = 0; attempt < AuthTestHarness.MaxFailedAttempts; attempt++)
            await this.harness.Login("not-the-password");

        Assert.That(await this.harness.Refresh(session.RefreshToken), Is.Null);
    }

    [Test]
    public async Task AnUnknownTokenIsRefusedWithoutTouchingAnything()
    {
        await this.harness.SignIn();

        var result = await this.harness.Refresh("this-was-never-issued");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Null);
            Assert.That(this.harness.RefreshTokens.All[0].RevokedAt, Is.Null);
        }
    }
}
