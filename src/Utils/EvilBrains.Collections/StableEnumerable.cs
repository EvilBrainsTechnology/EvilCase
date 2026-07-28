using System.Collections;

namespace EvilBrains.Collections;

internal sealed class StableEnumerable<T>(IEnumerable<T> internalEnumerable) : IStableEnumerable<T>
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

            this.DisposeInternalEnumerator();
        }

        return this.cache.AsReadOnly();
    }

    public void Dispose()
    {
        this.DisposeInternalEnumerator();
    }

    private IEnumerator<T> GetEnumeratorInternal()
    {
        var index = -1;

        while (true)
        {
            ++index;

            if (index < this.cache.Count)
            {
                yield return this.cache[index];
            }
            else if (this.internalEnumerator is { } enumerator && enumerator.MoveNext())
            {
                this.cache.Add(enumerator.Current);
                yield return enumerator.Current;
            }
            else
            {
                // The source may have been drained (and disposed) by another enumerator
                // while this one was suspended; only the items already cached remain.
                this.DisposeInternalEnumerator();
                yield break;
            }
        }
    }

    private void DisposeInternalEnumerator()
    {
        if (this.internalEnumerator is null)
            return;

        this.internalEnumerator.Dispose();
        this.internalEnumerator = null;
    }
}
