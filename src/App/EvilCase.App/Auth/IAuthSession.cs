namespace EvilBrains.EvilCase.App.Auth;

internal interface IAuthSession
{
    public Task<SignInOutcome> SignIn(string email, string password, CancellationToken token);

    /// <summary>
    /// Local state is dropped even when the API call fails.
    /// </summary>
    public Task SignOut(bool everywhere, CancellationToken token);

    /// <summary>
    /// Concurrent callers share one exchange: a caller whose <paramref name="seen"/> session has since been
    /// replaced by a fresh one gets that one. False where no session came back; the session is only dropped
    /// where the server rejected it.
    /// </summary>
    public Task<bool> Renew(SessionState? seen, CancellationToken token);

    /// <summary>
    /// Renews a session about to lapse, so a request the browser makes on its own carries a live access cookie.
    /// </summary>
    public Task EnsureFresh(CancellationToken token);

    /// <summary>
    /// Renews the session ahead of each lapse until <paramref name="token"/> is cancelled or the session ends, so
    /// a stream the browser keeps requesting on its own keeps a live access cookie.
    /// </summary>
    public Task KeepFresh(CancellationToken token);
}
