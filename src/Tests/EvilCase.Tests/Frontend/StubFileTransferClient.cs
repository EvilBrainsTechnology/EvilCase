using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// A render test never uploads or downloads a file; this internal interface cannot be
/// substituted with NSubstitute without a DynamicProxyGenAssembly2 visibility grant, so a plain
/// stub stands in instead.
/// </summary>
internal sealed class StubFileTransferClient : IFileTransferClient
{
    public Task UploadCaseFile(Guid caseId, IBrowserFile file, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public Task UploadActFile(Guid caseId, Guid actId, IBrowserFile file, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public Task<FileContent> DownloadFileContent(Guid fileId, CancellationToken token)
    {
        return Task.FromResult(new FileContent { MediaType = "application/octet-stream", Content = Stream.Null });
    }
}
