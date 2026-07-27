using System.Collections;

namespace EvilBrains.Collections;

internal sealed class StableEnumerable<T>(IEnumerable<T> internalEnumerable) : IStableEnumerable<T>, IDisposable
{
    private readonly IList<T> cache = [];

    private IEnumerator<T>? internalEnumerator = internalEnumerable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public IEnumerator<T> GetEnumerator()
    {
        if (this.internalEnumerator is null)
            return this.cache.GetEnumerator();

        return this.GetEnumeratorInternal();
    }

    public IReadOnlyList<T> AsReadOnlyList()
    {
        if (this.internalEnumerator is not null)
        {
            while (this.internalEnumerator.MoveNext())
                this.cache.Add(this.internalEnumerator.Current);

            this.DispoInternalEnumerator();
        }

        return this.cache.AsReadOnly();
    }

    public void Dispose()
    {
        this.DispoInternalEnumerator();
    }

    private IEnumerator<T> GetEnumeratorInternal()
    {
        var index = -1;

        while (this.internalEnumerator is not null)
        {
            ++index;

            if (index < this.cache.Count)
            {
                yield return this.cache[index];
            }
            else if (this.internalEnumerator.MoveNext())
            {
                this.cache.Add(this.internalEnumerator.Current);
                yield return this.internalEnumerator.Current;
            }
            else
            {
                this.DispoInternalEnumerator();
            }
        }
    }

    private void DispoInternalEnumerator()
    {
        if (this.internalEnumerator is null)
            return;

        this.internalEnumerator.Dispose();
        this.internalEnumerator = null;
    }
}
