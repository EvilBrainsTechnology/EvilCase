using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace EvilBrains.ApiClient.Generator;

internal readonly struct EquatableArray<T>(ImmutableArray<T> values) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> values = values;

    public int Count => this.values.IsDefault ? 0 : this.values.Length;

    public T this[int index] => this.values[index];

    public static bool operator ==(in EquatableArray<T> left, in EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(in EquatableArray<T> left, in EquatableArray<T> right)
    {
        return !left.Equals(right);
    }

    public bool Equals(EquatableArray<T> other)
    {
        if (this.Count != other.Count)
            return false;

        for (var i = 0; i < this.Count; i++)
        {
            if (!this.values[i].Equals(other.values[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && this.Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = 17;
        for (var i = 0; i < this.Count; i++)
            hash = (hash * 31) + this.values[i].GetHashCode();

        return hash;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)(this.values.IsDefault ? [] : this.values)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
