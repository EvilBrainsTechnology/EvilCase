namespace EvilBrains.Dispose;

public sealed class ActionDisposableScope(Action dispose) : IDisposable
{
    private Action? pending = dispose;

    public void Dispose()
    {
        var action = this.pending;

        this.pending = null;
        action?.Invoke();
    }
}
