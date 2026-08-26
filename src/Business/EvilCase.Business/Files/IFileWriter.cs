namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// Writes the files stored on a case, bytes and metadata together.
/// </summary>
public interface IFileWriter
{
    public Task<UploadFileResult> UploadCaseFile(Guid caseId, FileUpload upload, CancellationToken token);

    public Task<FileDeleteOutcome> DeleteCaseFile(Guid caseId, Guid fileId, CancellationToken token);
}
