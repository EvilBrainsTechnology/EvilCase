namespace EvilBrains.EvilCase.Business.Files;

public interface IFileWriter
{
    /// <summary>
    /// Stores one file on a case.
    /// </summary>
    public Task<UploadFileResult> UploadCaseFile(Guid caseId, FileUpload upload, CancellationToken token);

    public Task<FileDeleteOutcome> DeleteCaseFile(Guid caseId, Guid fileId, CancellationToken token);
}
