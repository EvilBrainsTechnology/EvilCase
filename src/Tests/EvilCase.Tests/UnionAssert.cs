using System.Runtime.CompilerServices;

namespace EvilBrains.EvilCase.Tests;

internal static class UnionAssert
{
    /// <summary>
    /// Asserts that the union holds a <typeparamref name="T"/> and returns it.
    /// </summary>
    public static T Holds<T>(IUnion union)
    {
        return union.Value is T value
            ? value
            : throw new AssertionException($"Expected the result to hold a {typeof(T).Name}, but it holds {union.Value ?? "nothing"}.");
    }
}
