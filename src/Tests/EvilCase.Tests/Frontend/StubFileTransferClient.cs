using EvilBrains.EvilCase.Api.Contract.Files;
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
    /// <summary>
    /// The files stored as new files, in upload order.
    /// </summary>
    public List<FileListItem> Uploaded { get; } = [];

    /// <summary>
    /// Every new file waits for it before it is stored.
    /// </summary>
    public Task Gate { get; set; } = Task.CompletedTask;

    public async Task<FileUploadResult> UploadCaseFile(Guid caseId, IBrowserFile file, CancellationToken token)
    {
        return await this.Upload(file);
    }

    public async Task<FileUploadResult> UploadActFile(Guid actId, IBrowserFile file, CancellationToken token)
    {
        return await this.Upload(file);
    }

    public Task<FileUploadResult> UploadFileVersion(Guid fileId, IBrowserFile file, CancellationToken token)
    {
        return Task.FromResult(Stored(file, FileUploadOutcome.NewVersion, 2));
    }

    public Task<Stream> DownloadFileContent(Guid fileId, int? version, CancellationToken token)
    {
        return Task.FromResult(Stream.Null);
    }

    private async Task<FileUploadResult> Upload(IBrowserFile file)
    {
        await this.Gate;

        var result = Stored(file, FileUploadOutcome.NewFile, 1);

        this.Uploaded.Add(result.File);

        return result;
    }

    private static FileUploadResult Stored(IBrowserFile file, FileUploadOutcome outcome, int version)
    {
        return new FileUploadResult
        {
            File = new FileListItem { FileId = Guid.CreateVersion7(), FileName = file.Name, SizeBytes = file.Size, Created = DateTime.UtcNow, Version = version, VersionCount = version },
            Outcome = outcome,
        };
    }
}
