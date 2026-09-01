using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;

namespace EvilBrains.EvilCase.Tests.Controllers;

internal sealed class RecordingFileReader : IFileReader
{
    public Guid? ListCaseId { get; private set; }

    public Guid? ListActId { get; private set; }

    public IReadOnlyList<FileListItem>? ListResult { get; init; }

    public FileDownload? Download { get; init; }

    public async Task<IReadOnlyList<FileListItem>?> ListCaseFiles(Guid caseId, CancellationToken token)
    {
        this.ListCaseId = caseId;

        return this.ListResult;
    }

    public async Task<IReadOnlyList<FileListItem>?> ListActFiles(Guid caseId, Guid actId, CancellationToken token)
    {
        this.ListCaseId = caseId;
        this.ListActId = actId;

        return this.ListResult;
    }

    public async Task<FileDownload?> OpenFileContent(Guid fileId, CancellationToken token)
    {
        return this.Download;
    }
}
