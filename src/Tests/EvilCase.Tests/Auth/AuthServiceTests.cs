using EvilBrains.EvilCase.Auth;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// Signing in: what separates a wrong password from an unknown account (nothing the caller can see),
/// and what the failure counter does on the way to a lockout.
/// </summary>
public class AuthServiceTests
{
    private const string WrongPassword = "not-the-password";

    private AuthTestHarness harness = null!;

    [SetUp]
    public void SetUp() => this.harness = new();

    [Test]
    public async Task SigningInReturnsBothTokensAndTheUsersRole()
    {
        var result = await this.harness.Login(AuthTestHarness.Password);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(LoginStatus.Success));
            Assert.That(result.Session?.AccessToken, Is.Not.Empty);
            Assert.That(result.Session?.RefreshToken, Is.Not.Empty);
            Assert.That(result.Session?.Email, Is.EqualTo(AuthTestHarness.Email));
            Assert.That(result.Session?.Role, Is.EqualTo(this.harness.User.Role));
            Assert.That(this.harness.RefreshTokens.All, Has.Count.EqualTo(1));
        }
    }

    /// <summary>
    /// The stored token is a hash, so the value handed to the browser must not be findable in the store.
    /// </summary>
    [Test]
    public async Task TheRefreshTokenIsNeverStoredAsGiven()
    {
        var session = await this.harness.SignIn();

        Assert.That(
            this.harness.RefreshTokens.All.Select(token => token.TokenHash),
            Has.No.Member(session.RefreshToken));
    }

    [Test]
    public async Task TheEmailIsMatchedRegardlessOfCaseAndSurroundingSpace()
    {
        var result = await this.harness.Service.Login(
            "  USER@EvilCase.TEST ",
            AuthTestHarness.Password,
            ClientInfo.Unknown,
            CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(LoginStatus.Success));
    }

    [Test]
    public async Task AnUnknownEmailIsRejectedTheSameWayAWrongPasswordIs()
    {
        var unknown = await this.harness.Service.Login(
            "nobody@evilcase.test",
            AuthTestHarness.Password,
            ClientInfo.Unknown,
            CancellationToken.None);

        var wrong = await this.harness.Login(WrongPassword);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unknown.Status, Is.EqualTo(LoginStatus.InvalidCredentials));
            Assert.That(wrong.Status, Is.EqualTo(LoginStatus.InvalidCredentials));
        }
    }

    [Test]
    public async Task AFailedAttemptCountsAndASuccessfulOneClearsTheCount()
    {
        _ = await this.harness.Login(WrongPassword);

        var afterFailure = this.harness.Users.Get(this.harness.User.Id).FailedLoginAttempts;

        _ = await this.harness.Login(AuthTestHarness.Password);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterFailure, Is.EqualTo(1));
            Assert.That(this.harness.Users.Get(this.harness.User.Id).FailedLoginAttempts, Is.Zero);
        }
    }

    [Test]
    public async Task TheAccountLocksOnTheConfiguredAttemptAndTheRightPasswordStopsWorking()
    {
        var statuses = new List<LoginStatus>();

        for (var attempt = 0; attempt < AuthTestHarness.MaxFailedAttempts; attempt++)
            statuses.Add((await this.harness.Login(WrongPassword)).Status);

        var withTheRightPassword = await this.harness.Login(AuthTestHarness.Password);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(statuses[..^1], Is.All.EqualTo(LoginStatus.InvalidCredentials));
            Assert.That(statuses[^1], Is.EqualTo(LoginStatus.LockedOut));
            Assert.That(withTheRightPassword.Status, Is.EqualTo(LoginStatus.LockedOut));
        }
    }

    [Test]
    public async Task TheLockoutElapsesOnItsOwn()
    {
        for (var attempt = 0; attempt < AuthTestHarness.MaxFailedAttempts; attempt++)
            _ = await this.harness.Login(WrongPassword);

        this.harness.Time.Advance(this.harness.Settings.Lockout.Duration + TimeSpan.FromSeconds(1));

        var result = await this.harness.Login(AuthTestHarness.Password);

        Assert.That(result.Status, Is.EqualTo(LoginStatus.Success));
    }

    /// <summary>
    /// The counter starts over with the lockout, or the first miss after it elapsed would lock the
    /// account straight back.
    /// </summary>
    [Test]
    public async Task OneMissAfterAnElapsedLockoutDoesNotLockAgain()
    {
        for (var attempt = 0; attempt < AuthTestHarness.MaxFailedAttempts; attempt++)
            _ = await this.harness.Login(WrongPassword);

        this.harness.Time.Advance(this.harness.Settings.Lockout.Duration + TimeSpan.FromSeconds(1));

        var result = await this.harness.Login(WrongPassword);

        Assert.That(result.Status, Is.EqualTo(LoginStatus.InvalidCredentials));
    }
}
