using EvilBrains.EvilCase.App.Auth;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// The sign-in page needs a session that answers with a prepared outcome; this internal interface
/// cannot be substituted with NSubstitute without a DynamicProxyGenAssembly2 visibility grant, so a
/// plain stub stands in instead.
/// </summary>
internal sealed class StubAuthSession : IAuthSession
{
    public SignInOutcome Outcome { get; set; } = SignInOutcome.Success;

    public int FreshnessChecks { get; private set; }

    public List<CancellationToken> KeptFresh { get; } = [];

    public Task<SignInOutcome> SignIn(string email, string password, CancellationToken token)
    {
        return Task.FromResult(this.Outcome);
    }

    public Task SignOut(bool everywhere, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public Task<bool> Renew(SessionState? seen, CancellationToken token)
    {
        return Task.FromResult(true);
    }

    public Task EnsureFresh(CancellationToken token)
    {
        this.FreshnessChecks++;

        return Task.CompletedTask;
    }

    public Task KeepFresh(CancellationToken token)
    {
        this.KeptFresh.Add(token);

        return Task.CompletedTask;
    }
}
