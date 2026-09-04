using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files;

public interface IFileReader
{
    /// <summary>
    /// Null where the case is unknown; empty where it has no files.
    /// </summary>
    public Task<IReadOnlyList<FileListItem>?> ListCaseFiles(Guid caseId, CancellationToken token);

    /// <summary>
    /// Null where the act is unknown; empty where it has no files.
    /// </summary>
    public Task<IReadOnlyList<FileListItem>?> ListActFiles(Guid caseId, Guid actId, CancellationToken token);

    /// <summary>
    /// Null where the file is unknown or its blob is gone.
    /// </summary>
    public Task<FileDownload?> OpenFileContent(Guid fileId, CancellationToken token);
}
