namespace EvilBrains.Collections;

public interface IStableEnumerable<out T> : IEnumerable<T>, IDisposable
{
    public IReadOnlyList<T> AsReadOnlyList();
}
