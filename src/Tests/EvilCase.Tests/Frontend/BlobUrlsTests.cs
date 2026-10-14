using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class BlobUrlsTests
{
    [Test]
    public async Task AUrlTheBrowserMadeAfterACancelStillReachesTheCaller()
    {
        var runtime = new PendingJSRuntime();
        await using var blobs = new BlobUrls(runtime);
        using var cancel = new CancellationTokenSource();

        var creating = blobs.CreateBlobUrl("application/pdf", new MemoryStream([1]), cancel.Token);
        await cancel.CancelAsync();
        runtime.Complete("blob:made");

        Assert.That(await creating, Is.EqualTo("blob:made"), "a URL the caller never receives is never revoked");
    }
}
