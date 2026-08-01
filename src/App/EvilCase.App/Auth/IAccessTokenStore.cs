namespace EvilBrains.EvilCase.App.Auth;

internal interface IAccessTokenStore
{
    public AccessTokenState? Current { get; }

    public void Set(AccessTokenState state);

    public void Clear();
}
