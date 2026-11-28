using EvilBrains.EvilCase.Business.Entities;

namespace EvilBrains.EvilCase.Business.Files;

public interface IFileWriter
{
    /// <summary>
    /// A name the act already has, ignoring case, makes the upload that file's next version.
    /// </summary>
    public Task<UploadFileResult> UploadActFile(Guid actId, FileUpload upload, CancellationToken token);

    public Task<FileVersionUploadResult> UploadFileVersion(Guid fileId, FileUpload upload, CancellationToken token);

    /// <summary>
    /// Stores the content of the numbered version as the file's next version, unless it equals the current one.
    /// </summary>
    public Task<FileVersionUploadResult> RestoreFileVersion(Guid fileId, int number, CancellationToken token);

    public Task<FileVersionDeleteOutcome> DeleteFileVersion(Guid fileId, int number, CancellationToken token);

    public Task<FileRenameOutcome> RenameFile(Guid fileId, string fileName, CancellationToken token);

    /// <summary>
    /// Moves the main file mark of the file's act to the file.
    /// </summary>
    public Task<MainFileOutcome> SetMainFile(Guid fileId, CancellationToken token);

    /// <summary>
    /// Leaves the file's act without a main file, if the file is it.
    /// </summary>
    public Task<MainFileOutcome> UnsetMainFile(Guid fileId, CancellationToken token);

    /// <summary>
    /// A deleted main file hands the mark to the oldest remaining file, unless another file took it meanwhile.
    /// </summary>
    public Task<DeleteOutcome> DeleteFile(Guid fileId, CancellationToken token);
}
