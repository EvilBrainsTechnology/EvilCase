namespace EvilBrains.EvilCase.App.Auth;

internal interface IAccessTokenStore
{
    public AccessTokenState? Current { get; }

    public void SetAccessToken(AccessTokenState state);

    public void ClearAccessToken();
}
