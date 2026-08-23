using EvilBrains.EvilCase.App.Search;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SearchDebouncerTests
{
    [Test]
    public async Task TheNewestSearchWinsWhenTwoStartsOverlap()
    {
        using var debouncer = new SearchDebouncer();

        var first = await debouncer.Start(debounce: false);

        // A live registration forces the next CancelAsync to yield, opening the re-entrancy window.
        await using var registration = first!.Value.Register(() =>
        {
        });

        var second = debouncer.Start(debounce: true);
        var third = debouncer.Start(debounce: true);

        var secondToken = await second;
        var thirdToken = await third;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondToken, Is.Null, "a start superseded before its delay returns null");
            Assert.That(thirdToken, Is.Not.Null, "the newest start keeps its token");
            Assert.That(thirdToken!.Value.IsCancellationRequested, Is.False, "the newest start's token stays live");
        }

        var next = await debouncer.Start(debounce: false);

        Assert.That(next!.Value.IsCancellationRequested, Is.False, "the debouncer stays usable after an overlap");
    }
}
