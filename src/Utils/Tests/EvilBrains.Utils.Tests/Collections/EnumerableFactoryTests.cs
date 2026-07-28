using EvilBrains.Collections.Factories;

namespace EvilBrains.Utils.Tests.Collections;

public class EnumerableFactoryTests
{
    [Test]
    public void SequenceTest()
    {
        Assert.That(Enumerable.Sequence(1, 5), Is.EqualTo(Enumerable.Range(1, 5)));
    }

    [Test]
    public void InfiniteSequenceIsLazyTest()
    {
        Assert.That(Enumerable.InfiniteSequence(0).Take(5), Is.EqualTo(Enumerable.Range(0, 5)));
    }
}
