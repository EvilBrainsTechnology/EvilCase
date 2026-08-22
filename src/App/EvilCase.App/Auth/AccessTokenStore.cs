namespace EvilBrains.EvilCase.App.Auth;

internal sealed class AccessTokenStore : IAccessTokenStore
{
    public AccessTokenState? Current { get; private set; }

    public void Set(AccessTokenState state)
    {
        this.Current = state;
    }

    public void Clear()
    {
        this.Current = null;
    }
}
