using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Files;

namespace EvilBrains.EvilCase.Tests.Controllers;

internal sealed class RecordingFileWriter : IFileWriter
{
    public bool UploadCalled { get; private set; }

    public Guid? UploadCaseId { get; private set; }

    public Guid? UploadActId { get; private set; }

    public FileUpload? Upload { get; private set; }

    public UploadFileResult UploadResult { get; init; } = new() { Outcome = UploadFileOutcome.OwnerNotFound };

    public Guid? DeleteCaseId { get; private set; }

    public Guid? DeleteActId { get; private set; }

    public Guid? DeleteFileId { get; private set; }

    public DeleteOutcome DeleteOutcome { get; init; }

    public async Task<UploadFileResult> UploadCaseFile(Guid caseId, FileUpload upload, CancellationToken token)
    {
        this.UploadCalled = true;
        this.UploadCaseId = caseId;
        this.Upload = upload;

        return this.UploadResult;
    }

    public async Task<UploadFileResult> UploadActFile(Guid caseId, Guid actId, FileUpload upload, CancellationToken token)
    {
        this.UploadCalled = true;
        this.UploadCaseId = caseId;
        this.UploadActId = actId;
        this.Upload = upload;

        return this.UploadResult;
    }

    public async Task<DeleteOutcome> DeleteCaseFile(Guid caseId, Guid fileId, CancellationToken token)
    {
        this.DeleteCaseId = caseId;
        this.DeleteFileId = fileId;

        return this.DeleteOutcome;
    }

    public async Task<DeleteOutcome> DeleteActFile(Guid caseId, Guid actId, Guid fileId, CancellationToken token)
    {
        this.DeleteCaseId = caseId;
        this.DeleteActId = actId;
        this.DeleteFileId = fileId;

        return this.DeleteOutcome;
    }
}
