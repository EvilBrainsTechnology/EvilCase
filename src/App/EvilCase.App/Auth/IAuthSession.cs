namespace EvilBrains.EvilCase.App.Auth;

/// <summary>
/// The browser's half of the session: signing in and out, and renewing the access token from the
/// refresh cookie. Implemented by the authentication state provider, which is what turns any of it into
/// a re-render.
/// </summary>
internal interface IAuthSession
{
    public Task<SignInOutcome> SignInAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Ends this session, or every session of this user. Local state is dropped either way, including
    /// when the call to the API fails.
    /// </summary>
    public Task SignOutAsync(bool everywhere, CancellationToken cancellationToken);

    /// <summary>
    /// Exchanges the refresh cookie for a new access token. Concurrent callers share one exchange.
    /// False where there is no usable session left.
    /// </summary>
    public Task<bool> RenewAsync(CancellationToken cancellationToken);
}
