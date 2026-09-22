using EvilBrains.EvilCase.App.Auth;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed class StubAuthSession : IAuthSession
{
    public SignInOutcome Outcome { get; set; } = SignInOutcome.Success;

    public List<(string Email, string Password)> Attempts { get; } = [];

    public Task<SignInOutcome> SignIn(string email, string password, CancellationToken token)
    {
        this.Attempts.Add((email, password));

        return Task.FromResult(this.Outcome);
    }

    public Task SignOut(bool everywhere, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public Task<bool> Renew(CancellationToken token)
    {
        return Task.FromResult(true);
    }
}
