using EvilBrains.EvilCase.Api.Contract.Files;
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
    public async Task<FileListItem?> UploadCaseFile(Guid caseId, FileUpload upload, CancellationToken token)
    {
        var context = dbSession.Current;

        var caseExists = await context.Cases.WithId(caseId).AnyAsync(token);
        if (!caseExists)
            return null;

        var fileAssetId = Guid.CreateVersion7();

        // The blob is written before the transaction commits; a blob orphaned by a failed write is tolerated (SDD-012).
        var blob = await blobStore.WriteFileBlob(userContext.TenantId, fileAssetId, upload.Content, token);

        var file = new FileAsset
        {
            Id = fileAssetId,
            CaseId = caseId,
            FileName = upload.FileName,
            ContentHash = blob.ContentHash,
            SizeBytes = blob.SizeBytes,
            StoragePath = blob.StoragePath,
            MediaType = upload.MediaType,
        };

        context.FileAssets.Add(file);
        await context.SaveChangesAsync(token);

        logger.LogInformation("File {FileAssetId} was stored on case {CaseId}, {SizeBytes} bytes", file.Id, caseId, file.SizeBytes);

        return new FileListItem
        {
            Id = file.Id,
            FileName = file.FileName,
            SizeBytes = file.SizeBytes,
            Created = file.Created,
        };
    }

    public async Task<FileDeleteOutcome> DeleteCaseFile(Guid caseId, Guid fileId, CancellationToken token)
    {
        var context = dbSession.Current;

        var file = await context.FileAssets
            .OfCase(caseId)
            .WithId(fileId)
            .SingleOrDefaultAsync(token);

        if (file is null)
            return FileDeleteOutcome.NotFound;

        context.FileAssets.Remove(file);
        await context.SaveChangesAsync(token);

        // The row goes first; a blob left behind is tolerated (SDD-012).
        await blobStore.DeleteFileBlob(file.StoragePath, token);

        logger.LogInformation("File {FileAssetId} was removed from case {CaseId}", file.Id, caseId);

        return FileDeleteOutcome.Deleted;
    }
}
