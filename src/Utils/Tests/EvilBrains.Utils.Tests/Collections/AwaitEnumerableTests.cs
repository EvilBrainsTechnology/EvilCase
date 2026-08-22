using EvilBrains.Collections;

namespace EvilBrains.Utils.Tests.Collections;

public class AwaitEnumerableTests
{
    [Test]
    public async Task AwaitEnumerableTest()
    {
        var values = await Enumerable
            .Range(2, 3)
            .Select(this.Foo)
            .AsReadOnlyCollectionAsync();

        Assert.That(values, Is.EqualTo(Enumerable.Range(2, 3)));
    }

    [Test]
    public async Task AwaitAsyncEnumerableTest()
    {
        var values = await Enumerable
            .Range(2, 3)
            .Select(this.Foo)
            .AsAsyncEnumerable()
            .Select(x => 2 * x)
            .AsReadOnlyCollectionAsync();

        Assert.That(values, Is.EqualTo(Enumerable.Range(2, 3).Select(x => 2 * x)));
    }

    private async Task<int> Foo(int number)
    {
        await Task.Delay(100 / number);
        return number;
    }
}
