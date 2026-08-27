using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// Reads the files a case or an act carries and the bytes behind one.
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// The files of one case, oldest first. Null where the tenant has no such case.
    /// </summary>
    public Task<IReadOnlyList<FileListItem>?> ListCaseFiles(Guid caseId, CancellationToken token);

    /// <summary>
    /// The files of one act of one case, oldest first. Null where the tenant has no such act on that case.
    /// </summary>
    public Task<IReadOnlyList<FileListItem>?> ListActFiles(Guid caseId, Guid actId, CancellationToken token);

    /// <summary>
    /// Opens the bytes of one file. Null where the tenant has no such file, or its blob is gone.
    /// </summary>
    public Task<FileDownload?> OpenFileContent(Guid fileId, CancellationToken token);
}
