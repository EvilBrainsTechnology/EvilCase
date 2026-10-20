namespace EvilBrains.EvilCase.App.Auth;

internal interface ISessionStore
{
    public SessionState? Current { get; }

    public void Set(SessionState state);

    public void Clear();
}
