namespace EvilBrains.EvilCase.App.Auth;

/// <summary>
/// The browser's half of the session: signing in and out, and renewing the access token from the
/// refresh cookie. Implemented by the authentication state provider, which is what turns any of it into
/// a re-render.
/// </summary>
internal interface IAuthSession
{
    public Task<SignInOutcome> SignIn(string email, string password, CancellationToken token);

    /// <summary>
    /// Ends this session, or every session of this user. Local state is dropped either way, including
    /// when the call to the API fails.
    /// </summary>
    public Task SignOut(bool everywhere, CancellationToken token);

    /// <summary>
    /// Exchanges the refresh cookie for a new access token. Concurrent callers share one exchange.
    /// False where no access token came back; the session is only dropped where the server rejected it.
    /// </summary>
    public Task<bool> Renew(CancellationToken token);
}
