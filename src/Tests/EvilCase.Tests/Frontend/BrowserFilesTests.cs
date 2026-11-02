using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class BrowserFilesTests
{
    [Test]
    public async Task AUrlTheBrowserMadeAfterACancelStillReachesTheCaller()
    {
        var runtime = new PendingJSRuntime();
        await using var files = new BrowserFiles(runtime, new StubAuthSession());
        using var cancel = new CancellationTokenSource();

        var creating = files.CreateBlobUrl("application/pdf", new MemoryStream([1]), cancel.Token);
        await cancel.CancelAsync();
        runtime.Complete("blob:made");

        Assert.That(await creating, Is.EqualTo("blob:made"), "a URL the caller never receives is never revoked");
    }
}
