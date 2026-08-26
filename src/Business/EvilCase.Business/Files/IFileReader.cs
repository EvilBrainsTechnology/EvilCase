using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files;

public interface IFileReader
{
    /// <summary>
    /// The files of one case, oldest first. Null where the tenant has no such case.
    /// </summary>
    public Task<IReadOnlyList<FileListItem>?> ListCaseFiles(Guid caseId, CancellationToken token);

    /// <summary>
    /// Opens the bytes of one file. Null where the tenant has no such file, or its blob is gone.
    /// </summary>
    public Task<FileDownload?> OpenFileContent(Guid fileId, CancellationToken token);
}
