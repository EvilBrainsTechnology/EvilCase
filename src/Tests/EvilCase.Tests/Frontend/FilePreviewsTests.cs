using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewsTests
{
    [Test]
    public async Task AConvertedPictureUrlMadeAfterACancelStillReachesTheCaller()
    {
        var runtime = new PendingJSRuntime();
        await using var previews = new FilePreviews(runtime);
        using var cancel = new CancellationTokenSource();

        var creating = previews.CreatePngUrl(new MemoryStream([1]), cancel.Token);
        await cancel.CancelAsync();
        runtime.Complete("blob:converted");

        Assert.That(await creating, Is.EqualTo("blob:converted"), "a URL the caller never receives is never revoked");
    }
}
