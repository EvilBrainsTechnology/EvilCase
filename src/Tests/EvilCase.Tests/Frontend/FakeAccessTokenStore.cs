using EvilBrains.EvilCase.App.Auth;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed class FakeAccessTokenStore : IAccessTokenStore
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
