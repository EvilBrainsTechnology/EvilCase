namespace EvilBrains.EvilCase.App.Auth;

internal sealed class SessionStore : ISessionStore
{
    public SessionState? Current { get; private set; }

    public void Set(SessionState state)
    {
        this.Current = state;
    }

    public void Clear()
    {
        this.Current = null;
    }
}
