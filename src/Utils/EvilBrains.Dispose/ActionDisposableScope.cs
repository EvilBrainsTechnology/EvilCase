namespace EvilBrains.Dispose;

/// <summary>
/// Runs one action when the scope ends. Disposing again runs nothing.
/// </summary>
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
