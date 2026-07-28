using EvilBrains.Collections;
using Moq;

namespace EvilBrains.Utils.Tests.Collections;

public class StableEnumerableTests
{
    public interface IGetNumberDummyService
    {
        public int GetNumber(int number);
    }

    [Test]
    public void StableEnumerableAsReadOnlyCollectionTest()
    {
        var mock = new Mock<IGetNumberDummyService>();
        mock.Setup(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive))).Returns<int>(input => input);

        var stableEnumerable = GetRange(mock.Object, 0, 5).AsStableEnumerable();
        var list1 = stableEnumerable.ToList();
        var list2 = stableEnumerable.ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list1, Is.EquivalentTo(Enumerable.Range(0, 5)));
            Assert.That(list2, Is.EquivalentTo(Enumerable.Range(0, 5)));
        }

        mock.Verify(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive)), Times.Exactly(5));
    }

    [Test]
    public void StableEnumerableEnumerateVsAsReadOnlyCollectionTest()
    {
        var mock = new Mock<IGetNumberDummyService>();
        mock.Setup(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive))).Returns<int>(input => input);

        var stableEnumerable = GetRange(mock.Object, 0, 5).AsStableEnumerable();
        var list1 = stableEnumerable.ToList();
        var list2 = stableEnumerable.Select(x => x).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list1, Is.EquivalentTo(Enumerable.Range(0, 5)));
            Assert.That(list2, Is.EquivalentTo(Enumerable.Range(0, 5)));
        }

        mock.Verify(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive)), Times.Exactly(5));
    }

    [Test]
    public void StableEnumerableEnumerateVsAsReadOnlyCollectionTest2()
    {
        var mock = new Mock<IGetNumberDummyService>();
        mock.Setup(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive))).Returns<int>(input => input);

        var stableEnumerable = GetRange(mock.Object, 0, 5).AsStableEnumerable();
        var list1 = stableEnumerable.Select(x => x).ToList();
        var list2 = stableEnumerable.ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list1, Is.EquivalentTo(Enumerable.Range(0, 5)));
            Assert.That(list2, Is.EquivalentTo(Enumerable.Range(0, 5)));
        }

        mock.Verify(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive)), Times.Exactly(5));
    }

    [Test]
    public void StableEnumerablePartialEnumerationTest()
    {
        var mock = new Mock<IGetNumberDummyService>();
        mock.Setup(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive))).Returns<int>(input => input);

        var stableEnumerable = GetRange(mock.Object, 0, 5).AsStableEnumerable();
        var list1 = stableEnumerable.Take(3).ToList();
        var list2 = stableEnumerable.ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list1, Is.EquivalentTo(Enumerable.Range(0, 3)));
            Assert.That(list2, Is.EquivalentTo(Enumerable.Range(0, 5)));
        }

        mock.Verify(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive)), Times.Exactly(5));
    }

    [Test]
    public void StableEnumerablePartialEnumerationTest2()
    {
        var mock = new Mock<IGetNumberDummyService>();
        mock.Setup(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive))).Returns<int>(input => input);

        var stableEnumerable = GetRange(mock.Object, 0, 5).AsStableEnumerable();
        var list1 = stableEnumerable.Take(3).ToList();
        var list2 = stableEnumerable.Select(x => x).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(list1, Is.EquivalentTo(Enumerable.Range(0, 3)));
            Assert.That(list2, Is.EquivalentTo(Enumerable.Range(0, 5)));
        }

        mock.Verify(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive)), Times.Exactly(5));
    }

    [Test]
    public void StableEnumerableInterleavedEnumeratorsTest()
    {
        var mock = new Mock<IGetNumberDummyService>();
        mock.Setup(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive))).Returns<int>(input => input);

        var stableEnumerable = GetRange(mock.Object, 0, 5).AsStableEnumerable();

        using var enumerator = stableEnumerable.GetEnumerator();
        Assert.That(enumerator.MoveNext(), Is.True);

        var drained = stableEnumerable.ToList();

        var remaining = new List<int> { enumerator.Current };
        while (enumerator.MoveNext())
            remaining.Add(enumerator.Current);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(drained, Is.EqualTo(Enumerable.Range(0, 5)));
            Assert.That(remaining, Is.EqualTo(Enumerable.Range(0, 5)));
        }

        mock.Verify(dummyService => dummyService.GetNumber(It.IsInRange(0, 4, Moq.Range.Inclusive)), Times.Exactly(5));
    }

    private static IEnumerable<int> GetRange(IGetNumberDummyService dummyService, int start, int count)
    {
        for (var i = start; i < start + count; ++i)
        {
            Console.WriteLine($"Generating {i} value");
            yield return dummyService.GetNumber(i);
        }
    }
}
