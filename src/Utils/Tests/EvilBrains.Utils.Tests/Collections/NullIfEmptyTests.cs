using EvilBrains.Collections;
using EvilBrains.Collections.Factories;

namespace EvilBrains.Utils.Tests.Collections;

public class NullIfEmptyTests
{
    [Test]
    public void StringNullIfEmptyTest()
    {
        const string str = "12345";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(((string?)null).NullIfEmpty(), Is.Null);
            Assert.That(string.Empty.NullIfEmpty(), Is.Null);
            Assert.That(str.NullIfEmpty(), Is.EquivalentTo(str));
            Assert.That(str.NullIfEmpty(), Is.TypeOf<string>());
        }
    }

    [Test]
    public void CollectionNullIfEmptyTest()
    {
        var emptyList = List.Empty<int>();
        var list = List.From(1, 2, 3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(((IList<int>?)null).NullIfEmpty(), Is.Null);
            Assert.That(emptyList.NullIfEmpty(), Is.Null);
            Assert.That(list.NullIfEmpty(), Is.SameAs(list));
        }
    }

    [Test]
    public void EnumerableNullIfEmptyTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(((IEnumerable<int>?)null).NullIfEmpty(), Is.Null);
            Assert.That(Enumerable.Empty<int>().NullIfEmpty(), Is.Null);

#pragma warning disable CS8620 // remove this when fixed in Roslyn - https://github.com/dotnet/roslyn/issues/80024
            Assert.That(Enumerable.From(1, 2, 3).NullIfEmpty(), Is.EquivalentTo(Enumerable.From(1, 2, 3)));
#pragma warning restore CS8620

            Assert.Throws<InvalidOperationException>(() => FailAfter(0).NullIfEmpty());
            Assert.DoesNotThrow(() => FailAfter(1).NullIfEmpty());
        }
    }

    [Test]
    public void EnumerableNullIfEmptyRepeatedEnumerationTest()
    {
        var result = LazyRange(3).NullIfEmpty();

        Assert.That(result, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(Enumerable.Range(0, 3)));
            Assert.That(result, Is.EqualTo(Enumerable.Range(0, 3)));
        }
    }

    private static IEnumerable<int> LazyRange(int count)
    {
        for (var i = 0; i < count; i++)
            yield return i;
    }

    private static IEnumerable<int> FailAfter(int count)
    {
        for (var i = 0; i < count; i++)
            yield return i;

        throw new InvalidOperationException();
    }
}
