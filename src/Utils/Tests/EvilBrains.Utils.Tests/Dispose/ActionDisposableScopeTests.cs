using EvilBrains.Dispose;

namespace EvilBrains.Utils.Tests.Dispose;

public sealed class ActionDisposableScopeTests
{
    [Test]
    public void TheActionRunsWhenTheScopeEnds()
    {
        var calls = 0;

        using (new ActionDisposableScope(() => calls++))
            Assert.That(calls, Is.Zero);

        Assert.That(calls, Is.EqualTo(1), "the scope runs its action when it ends");
    }

    [Test]
    public void DisposingTwiceRunsTheActionOnce()
    {
        var calls = 0;
        var scope = new ActionDisposableScope(() => calls++);

        scope.Dispose();
        scope.Dispose();

        Assert.That(calls, Is.EqualTo(1), "a second dispose runs nothing");
    }
}
