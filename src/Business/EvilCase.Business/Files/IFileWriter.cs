using EvilBrains.EvilCase.Business.Entities;

namespace EvilBrains.EvilCase.Business.Files;

public interface IFileWriter
{
    public Task<UploadFileResult> UploadCaseFile(Guid caseId, FileUpload upload, CancellationToken token);

    public Task<UploadFileResult> UploadActFile(Guid actId, FileUpload upload, CancellationToken token);

    public Task<FileRenameOutcome> RenameCaseFile(Guid caseId, Guid fileId, string fileName, CancellationToken token);

    public Task<FileRenameOutcome> RenameActFile(Guid actId, Guid fileId, string fileName, CancellationToken token);

    public Task<DeleteOutcome> DeleteCaseFile(Guid caseId, Guid fileId, CancellationToken token);

    public Task<DeleteOutcome> DeleteActFile(Guid actId, Guid fileId, CancellationToken token);
}
