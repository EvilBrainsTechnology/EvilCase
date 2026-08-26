using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files;

public interface IFileWriter
{
    /// <summary>
    /// Stores one file on a case. Null where the tenant has no such case.
    /// </summary>
    public Task<FileListItem?> UploadCaseFile(Guid caseId, FileUpload upload, CancellationToken token);

    public Task<FileDeleteOutcome> DeleteCaseFile(Guid caseId, Guid fileId, CancellationToken token);
}
