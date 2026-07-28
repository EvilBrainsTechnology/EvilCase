using EvilBrains.Collections;
using EvilBrains.Collections.Factories;

namespace EvilBrains.Utils.Tests.Collections;

public class EmptyIfNullTests
{
    [Test]
    public void StringEmptyIfNullTest()
    {
        const string str = "12345";
        const string emptyString = "";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(((string?)null).EmptyIfNull(), Is.Empty);
            Assert.That(emptyString.EmptyIfNull(), Is.SameAs(emptyString));
            Assert.That(str.EmptyIfNull(), Is.SameAs(str));
        }
    }

    [Test]
    public void ListEmptyIfNullTest()
    {
        var emptyList = List.Empty<int>();
        var list = List.From(1, 2, 3);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(((List<int>?)null).EmptyIfNull(), Is.Empty);
            Assert.That(emptyList.EmptyIfNull(), Is.SameAs(emptyList));
            Assert.That(list.EmptyIfNull(), Is.SameAs(list));
        }
    }

    [Test]
    public void DictionaryEmptyIfNullTest()
    {
        var emptyDictionary = ReadOnlyDictionary.Empty<int, string>();
        var dictionary = new Dictionary<int, string> { [1] = "one" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(((IReadOnlyDictionary<int, string>?)null).EmptyIfNull(), Is.Empty);
            Assert.That(emptyDictionary.EmptyIfNull(), Is.SameAs(emptyDictionary));
            Assert.That(dictionary.EmptyIfNull(), Is.SameAs(dictionary));
        }
    }

    [Test]
    public void EnumerableEmptyIfNullTest()
    {
        var enumerable = Enumerable.Range(0, 5);
        var emptyEnumerable = Enumerable.Empty<int>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(((IEnumerable<int>?)null).EmptyIfNull(), Is.Empty);
            Assert.That(emptyEnumerable.EmptyIfNull(), Is.SameAs(emptyEnumerable));

            Assert.That(enumerable.EmptyIfNull(), Is.SameAs(enumerable));

            Assert.DoesNotThrow(() => FailAfter(0).EmptyIfNull());
        }
    }

    private static IEnumerable<int> FailAfter(int count)
    {
        for (var i = 0; i < count; i++)
            yield return i;

        throw new InvalidOperationException();
    }
}
