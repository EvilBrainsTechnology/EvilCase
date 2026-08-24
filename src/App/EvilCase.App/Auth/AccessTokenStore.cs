namespace EvilBrains.EvilCase.App.Auth;

internal sealed class AccessTokenStore : IAccessTokenStore
{
    public AccessTokenState? Current { get; private set; }

    public void SetAccessToken(AccessTokenState state)
    {
        this.Current = state;
    }

    public void ClearAccessToken()
    {
        this.Current = null;
    }
}
