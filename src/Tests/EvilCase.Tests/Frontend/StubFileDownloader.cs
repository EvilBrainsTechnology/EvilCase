using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// A render test never downloads a file; this internal interface cannot be substituted with
/// NSubstitute without a DynamicProxyGenAssembly2 visibility grant, so a plain stub stands in
/// instead.
/// </summary>
internal sealed class StubFileDownloader : IFileDownloader
{
    public Task SaveFile(string fileName, FileContent content, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}
