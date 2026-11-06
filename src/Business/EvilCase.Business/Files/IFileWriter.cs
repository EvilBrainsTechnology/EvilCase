using EvilBrains.EvilCase.Business.Entities;

namespace EvilBrains.EvilCase.Business.Files;

public interface IFileWriter
{
    public Task<UploadFileResult> UploadCaseFile(Guid caseId, FileUpload upload, CancellationToken token);

    public Task<UploadFileResult> UploadActFile(Guid actId, FileUpload upload, CancellationToken token);

    public Task<FileVersionUploadResult> UploadFileVersion(Guid fileId, FileUpload upload, CancellationToken token);

    /// <summary>
    /// Stores the content of the numbered version as the file's next version, unless it equals the current one.
    /// </summary>
    public Task<FileVersionUploadResult> RestoreFileVersion(Guid fileId, int number, CancellationToken token);

    public Task<FileVersionDeleteOutcome> DeleteFileVersion(Guid fileId, int number, CancellationToken token);

    public Task<FileRenameOutcome> RenameCaseFile(Guid caseId, Guid fileId, string fileName, CancellationToken token);

    public Task<FileRenameOutcome> RenameActFile(Guid actId, Guid fileId, string fileName, CancellationToken token);

    /// <summary>
    /// Moves the main file mark of the file's case or act to the file.
    /// </summary>
    public Task<MainFileOutcome> SetMainFile(Guid fileId, CancellationToken token);

    /// <summary>
    /// Leaves the file's case or act without a main file, if the file is it.
    /// </summary>
    public Task<MainFileOutcome> UnsetMainFile(Guid fileId, CancellationToken token);

    public Task<DeleteOutcome> DeleteCaseFile(Guid caseId, Guid fileId, CancellationToken token);

    public Task<DeleteOutcome> DeleteActFile(Guid actId, Guid fileId, CancellationToken token);
}
