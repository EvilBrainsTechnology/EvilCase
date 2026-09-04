namespace EvilBrains.EvilCase.App.Auth;

internal interface IAuthSession
{
    public Task<SignInOutcome> SignIn(string email, string password, CancellationToken token);

    /// <summary>
    /// Local state is dropped even when the API call fails.
    /// </summary>
    public Task SignOut(bool everywhere, CancellationToken token);

    /// <summary>
    /// Concurrent callers share one exchange. False where no access token came back; the session is only
    /// dropped where the server rejected it.
    /// </summary>
    public Task<bool> Renew(CancellationToken token);
}
