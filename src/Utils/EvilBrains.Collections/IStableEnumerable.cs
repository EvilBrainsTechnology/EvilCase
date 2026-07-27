namespace EvilBrains.Collections;

public interface IStableEnumerable<out T> : IEnumerable<T>
{
    public IReadOnlyList<T> AsReadOnlyList();
}
