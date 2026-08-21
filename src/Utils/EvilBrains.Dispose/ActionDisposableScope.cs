namespace EvilBrains.Dispose;

/// <summary>
/// Runs one action when the scope ends. Disposing again runs nothing.
/// </summary>
public sealed class ActionDisposableScope : IDisposable
{
    private Action? dispose;

    public ActionDisposableScope(Action dispose)
    {
        ArgumentNullException.ThrowIfNull(dispose);

        this.dispose = dispose;
    }

    public void Dispose()
    {
        var action = this.dispose;

        this.dispose = null;
        action?.Invoke();
    }
}
