using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;

namespace EvilBrains.EvilCase.Tests.Controllers;

internal sealed class RecordingFileReader : IFileReader
{
    public Guid? ListCaseId { get; private set; }

    public Guid? ListActId { get; private set; }

    public Guid? DownloadFileId { get; private set; }

    public IReadOnlyList<FileListItem>? ListResult { get; init; }

    public FileDownload? Download { get; init; }

    public Task<IReadOnlyList<FileListItem>?> ListCaseFiles(Guid caseId, CancellationToken token)
    {
        this.ListCaseId = caseId;

        return Task.FromResult(this.ListResult);
    }

    public Task<IReadOnlyList<FileListItem>?> ListActFiles(Guid caseId, Guid actId, CancellationToken token)
    {
        this.ListCaseId = caseId;
        this.ListActId = actId;

        return Task.FromResult(this.ListResult);
    }

    public Task<FileDownload?> OpenFileContent(Guid fileId, CancellationToken token)
    {
        this.DownloadFileId = fileId;

        return Task.FromResult(this.Download);
    }
}
