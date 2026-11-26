using EvilBrains.EvilCase.Auth;

namespace EvilBrains.EvilCase.Tests.Auth;

public class AuthServiceTests
{
    private const string WrongPassword = "not-the-password";

    private AuthTestHarness harness = null!;

    [SetUp]
    public void SetUp()
    {
        this.harness = new();
    }

    [Test]
    public async Task SigningInReturnsBothTokensAndTheUsersRole()
    {
        var session = UnionAssert.Holds<AuthSession>(await this.harness.Login(AuthTestHarness.Password));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.AccessToken, Is.Not.Empty);
            Assert.That(session.RefreshToken, Is.Not.Empty);
            Assert.That(session.Email, Is.EqualTo(AuthTestHarness.Email));
            Assert.That(session.Role, Is.EqualTo(this.harness.User.Role));
            Assert.That(this.harness.RefreshTokens.All, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task TheRefreshTokenIsNeverStoredAsGiven()
    {
        var session = await this.harness.SignIn();

        Assert.That(
            this.harness.RefreshTokens.All.Select(static token => token.TokenHash),
            Has.No.Member(session.RefreshToken));
    }

    [Test]
    public async Task TheEmailIsMatchedRegardlessOfCaseAndSurroundingSpace()
    {
        var result = await this.harness.Service.Login(
            "  USER@EvilCase.TEST ",
            AuthTestHarness.Password,
            new(),
            CancellationToken.None);

        Assert.That(result.Value, Is.TypeOf<AuthSession>());
    }

    [Test]
    public async Task AnUnknownEmailIsRejectedTheSameWayAWrongPasswordIs()
    {
        var unknown = await this.harness.Service.Login(
            "nobody@evilcase.test",
            AuthTestHarness.Password,
            new(),
            CancellationToken.None);

        var wrong = await this.harness.Login(WrongPassword);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unknown.Value, Is.EqualTo(LoginFailure.InvalidCredentials));
            Assert.That(wrong.Value, Is.EqualTo(LoginFailure.InvalidCredentials));
        }
    }

    [Test]
    public async Task AFailedAttemptCountsAndASuccessfulOneClearsTheCount()
    {
        await this.harness.Login(WrongPassword);

        var afterFailure = this.harness.Users.GetUser(this.harness.User.Id).FailedLoginAttempts;

        await this.harness.Login(AuthTestHarness.Password);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterFailure, Is.EqualTo(1));
            Assert.That(this.harness.Users.GetUser(this.harness.User.Id).FailedLoginAttempts, Is.Zero);
        }
    }

    [Test]
    public async Task TheAccountLocksOnTheConfiguredAttemptAndTheRightPasswordStopsWorking()
    {
        var failures = new List<LoginFailure>();

        for (var attempt = 0; attempt < AuthTestHarness.MaxFailedAttempts; attempt++)
            failures.Add(UnionAssert.Holds<LoginFailure>(await this.harness.Login(WrongPassword)));

        var withTheRightPassword = await this.harness.Login(AuthTestHarness.Password);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failures[..^1], Is.All.EqualTo(LoginFailure.InvalidCredentials));
            Assert.That(failures[^1], Is.EqualTo(LoginFailure.LockedOut));
            Assert.That(withTheRightPassword.Value, Is.EqualTo(LoginFailure.LockedOut));
        }
    }

    [Test]
    public async Task AnAttemptThatReadTheAccountBeforeItLockedDoesNotGetToVerify()
    {
        var beforeTheLockout = this.harness.Users.GetUser(this.harness.User.Id);

        for (var attempt = 0; attempt < AuthTestHarness.MaxFailedAttempts; attempt++)
            await this.harness.Login(WrongPassword);

        this.harness.Users.StaleRead = beforeTheLockout;

        var result = await this.harness.Login(AuthTestHarness.Password);

        Assert.That(
            result.Value,
            Is.EqualTo(LoginFailure.LockedOut),
            "a burst read the row before its fifth miss locked it, and the right password in it must not sign in");
    }

    [Test]
    public async Task TheLockoutElapsesOnItsOwn()
    {
        for (var attempt = 0; attempt < AuthTestHarness.MaxFailedAttempts; attempt++)
            await this.harness.Login(WrongPassword);

        this.harness.Time.Advance(this.harness.Settings.Lockout.Duration + TimeSpan.FromSeconds(1));

        var result = await this.harness.Login(AuthTestHarness.Password);

        Assert.That(result.Value, Is.TypeOf<AuthSession>());
    }

    [Test]
    public async Task OneMissAfterAnElapsedLockoutDoesNotLockAgain()
    {
        for (var attempt = 0; attempt < AuthTestHarness.MaxFailedAttempts; attempt++)
            await this.harness.Login(WrongPassword);

        this.harness.Time.Advance(this.harness.Settings.Lockout.Duration + TimeSpan.FromSeconds(1));

        var result = await this.harness.Login(WrongPassword);

        Assert.That(result.Value, Is.EqualTo(LoginFailure.InvalidCredentials));
    }
}
