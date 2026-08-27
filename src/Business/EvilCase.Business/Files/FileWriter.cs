using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Files;

internal sealed class FileWriter(IDbSession dbSession, IFileBlobStore blobStore, IUserContext userContext, ILogger<FileWriter> logger) : IFileWriter
{
    public async Task<UploadFileResult> UploadCaseFile(Guid caseId, FileUpload upload, CancellationToken token)
    {
        var caseExists = await dbSession.Current.Cases.WithId(caseId).AnyAsync(token);
        if (!caseExists)
            return new UploadFileResult { Outcome = UploadFileOutcome.OwnerNotFound };

        var file = await this.StoreFile(caseId, actId: null, upload, token);

        logger.LogInformation("File {FileAssetId} was stored on case {CaseId}, {SizeBytes} bytes", file.FileId, caseId, file.SizeBytes);

        return new UploadFileResult { Outcome = UploadFileOutcome.Uploaded, File = file };
    }

    public async Task<UploadFileResult> UploadActFile(Guid caseId, Guid actId, FileUpload upload, CancellationToken token)
    {
        var actExists = await dbSession.Current.Acts.OfCase(caseId).WithId(actId).AnyAsync(token);
        if (!actExists)
            return new UploadFileResult { Outcome = UploadFileOutcome.OwnerNotFound };

        var file = await this.StoreFile(caseId: null, actId, upload, token);

        logger.LogInformation("File {FileAssetId} was stored on act {ActId}, {SizeBytes} bytes", file.FileId, actId, file.SizeBytes);

        return new UploadFileResult { Outcome = UploadFileOutcome.Uploaded, File = file };
    }

    public async Task<FileDeleteOutcome> DeleteCaseFile(Guid caseId, Guid fileId, CancellationToken token)
    {
        var context = dbSession.Current;

        // Read before the delete: the row is gone once ExecuteDeleteAsync runs.
        var storagePath = await context.FileAssets
            .OfCase(caseId)
            .WithId(fileId)
            .Select(file => file.StoragePath)
            .SingleOrDefaultAsync(token);

        if (storagePath is null)
            return FileDeleteOutcome.NotFound;

        await context.FileAssets
            .OfCase(caseId)
            .WithId(fileId)
            .ExecuteDeleteAsync(token);

        // The row goes first; a blob left behind is tolerated (SDD-012).
        await blobStore.DeleteFileBlob(storagePath, token);

        logger.LogInformation("File {FileAssetId} was removed from case {CaseId}", fileId, caseId);

        return FileDeleteOutcome.Deleted;
    }

    public async Task<FileDeleteOutcome> DeleteActFile(Guid caseId, Guid actId, Guid fileId, CancellationToken token)
    {
        var context = dbSession.Current;

        // Read before the delete: the row is gone once ExecuteDeleteAsync runs.
        var storagePath = await context.FileAssets
            .OfAct(caseId, actId)
            .WithId(fileId)
            .Select(file => file.StoragePath)
            .SingleOrDefaultAsync(token);

        if (storagePath is null)
            return FileDeleteOutcome.NotFound;

        await context.FileAssets
            .OfAct(caseId, actId)
            .WithId(fileId)
            .ExecuteDeleteAsync(token);

        // The row goes first; a blob left behind is tolerated (SDD-012).
        await blobStore.DeleteFileBlob(storagePath, token);

        logger.LogInformation("File {FileAssetId} was removed from act {ActId}", fileId, actId);

        return FileDeleteOutcome.Deleted;
    }

    private async Task<FileListItem> StoreFile(Guid? caseId, Guid? actId, FileUpload upload, CancellationToken token)
    {
        var context = dbSession.Current;

        var fileAssetId = Guid.CreateVersion7();

        // The blob is written before the transaction commits; a blob orphaned by a failed write is tolerated (SDD-012).
        var blob = await blobStore.WriteFileBlob(userContext.TenantId, fileAssetId, upload.Content, token);

        var file = new FileAsset
        {
            Id = fileAssetId,
            CaseId = caseId,
            ActId = actId,
            FileName = upload.FileName,
            ContentHash = blob.ContentHash,
            SizeBytes = blob.SizeBytes,
            StoragePath = blob.StoragePath,
            MediaType = upload.MediaType,
        };

        context.FileAssets.Add(file);
        await context.SaveChangesAsync(token);

        return new FileListItem
        {
            FileId = file.Id,
            FileName = file.FileName,
            SizeBytes = file.SizeBytes,
            Created = file.Created,
        };
    }
}
