using System.Data.Common;
using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Files;

internal sealed class FileWriter(IDbSession dbSession, IFileBlobStore blobStore, IUserContext userContext, ILogger<FileWriter> logger) : IFileWriter
{
    /// <summary>
    /// The unique indexes settle a race for a file name or a version number; a loser retries.
    /// </summary>
    private const int Attempts = 5;

    public async Task<UploadFileResult> UploadCaseFile(Guid caseId, FileUpload upload, CancellationToken token)
    {
        var context = dbSession.Current;

        var caseExists = await context.Cases.Exists(caseId, token);
        if (!caseExists)
            return new UploadFileResult { Outcome = UploadFileOutcome.OwnerNotFound };

        var result = await this.StoreUpload(context.FileAssets.OfCase(caseId), caseId, actId: null, upload, token);
        if (result.Outcome == UploadFileOutcome.Uploaded)
            logger.LogInformation("File {FileAssetId} was stored on case {CaseId}, {SizeBytes} bytes", result.File!.FileId, caseId, result.File.SizeBytes);

        return result;
    }

    public async Task<UploadFileResult> UploadActFile(Guid actId, FileUpload upload, CancellationToken token)
    {
        var context = dbSession.Current;

        var actExists = await context.Acts.Exists(actId, token);
        if (!actExists)
            return new UploadFileResult { Outcome = UploadFileOutcome.OwnerNotFound };

        var result = await this.StoreUpload(context.FileAssets.OfAct(actId), caseId: null, actId, upload, token);
        if (result.Outcome == UploadFileOutcome.Uploaded)
            logger.LogInformation("File {FileAssetId} was stored on act {ActId}, {SizeBytes} bytes", result.File!.FileId, actId, result.File.SizeBytes);

        return result;
    }

    public async Task<FileVersionUploadResult> UploadFileVersion(Guid fileId, FileUpload upload, CancellationToken token)
    {
        var context = dbSession.Current;

        var fileName = await context.FileAssets
            .WithId(fileId)
            .Select(static file => file.FileName)
            .SingleOrDefaultAsync(token);

        if (fileName is null)
            return new FileVersionUploadResult { Outcome = FileVersionUploadOutcome.FileNotFound };

        if (!FileNameRules.SameExtension(fileName, upload.FileName))
            return new FileVersionUploadResult { Outcome = FileVersionUploadOutcome.ExtensionMismatch };

        var fileVersionId = Guid.CreateVersion7();

        // The blob is written before the row it backs; a blob orphaned by a failed write or an unchanged upload is tolerated (SDD-012).
        var blob = await blobStore.WriteFileBlob(userContext.TenantId, fileVersionId, upload.Content, token);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var outcome = await this.SaveNextVersion(fileId, fileVersionId, upload.MediaType, blob, token);
                if (outcome == FileVersionUploadOutcome.FileNotFound)
                    return new FileVersionUploadResult { Outcome = outcome };

                return new FileVersionUploadResult { Outcome = outcome, File = await this.ReadListItem(fileId, token) };
            }
            catch (DbUpdateException exception) when (attempt < Attempts && exception.IsUniqueViolation())
            {
                this.ForgetLostRace();
            }
        }
    }

    public async Task<FileVersionDeleteOutcome> DeleteFileVersion(Guid fileId, int number, CancellationToken token)
    {
        var context = dbSession.Current;

        await using var transaction = await dbSession.BeginTransaction(token);

        // A no-op update locks the file's row, so two deletes of its last two versions run one after the other.
        var files = await context.FileAssets
            .WithId(fileId)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static file => file.FileName, static file => file.FileName), token);

        if (files == 0)
            return FileVersionDeleteOutcome.NotFound;

        var versions = context.FileVersions.OfFile(fileId);

        if (!await versions.AnyAsync(version => version.Number == number, token))
            return FileVersionDeleteOutcome.NotFound;

        if (await versions.CountAsync(token) == 1)
            return FileVersionDeleteOutcome.LastVersion;

        await versions
            .Where(version => version.Number == number)
            .ExecuteDeleteAsync(token);

        await transaction.CommitAsync(token);

        logger.LogInformation("File version {FileVersionNumber} of {FileAssetId} was removed", number, fileId);

        return FileVersionDeleteOutcome.Deleted;
    }

    public async Task<FileRenameOutcome> RenameCaseFile(Guid caseId, Guid fileId, string fileName, CancellationToken token)
    {
        var files = dbSession.Current.FileAssets.OfCase(caseId);

        var outcome = await RenameFile(files, fileId, fileName, token);
        if (outcome == FileRenameOutcome.Renamed)
            logger.LogInformation("File {FileAssetId} was renamed on case {CaseId}", fileId, caseId);

        return outcome;
    }

    public async Task<FileRenameOutcome> RenameActFile(Guid actId, Guid fileId, string fileName, CancellationToken token)
    {
        var files = dbSession.Current.FileAssets.OfAct(actId);

        var outcome = await RenameFile(files, fileId, fileName, token);
        if (outcome == FileRenameOutcome.Renamed)
            logger.LogInformation("File {FileAssetId} was renamed on act {ActId}", fileId, actId);

        return outcome;
    }

    public async Task<DeleteOutcome> DeleteCaseFile(Guid caseId, Guid fileId, CancellationToken token)
    {
        var files = dbSession.Current.FileAssets.OfCase(caseId).WithId(fileId);

        var outcome = await DeleteFile(files, token);
        if (outcome == DeleteOutcome.Deleted)
            logger.LogInformation("File {FileAssetId} was removed from case {CaseId}", fileId, caseId);

        return outcome;
    }

    public async Task<DeleteOutcome> DeleteActFile(Guid actId, Guid fileId, CancellationToken token)
    {
        var files = dbSession.Current.FileAssets.OfAct(actId).WithId(fileId);

        var outcome = await DeleteFile(files, token);
        if (outcome == DeleteOutcome.Deleted)
            logger.LogInformation("File {FileAssetId} was removed from act {ActId}", fileId, actId);

        return outcome;
    }

    // TenantId and UserId are stamped by UserWriteInterceptor.
    private static FileVersion NewVersion(Guid fileAssetId, int number, Guid fileVersionId, string? mediaType, FileBlobInfo blob)
    {
        return new FileVersion
        {
            Id = fileVersionId,
            FileAssetId = fileAssetId,
            Number = number,
            ContentHash = blob.ContentHash,
            SizeBytes = blob.SizeBytes,
            MediaType = mediaType,
            StoragePath = blob.StoragePath,
        };
    }

    private static async Task<FileRenameOutcome> RenameFile(IQueryable<FileAsset> ownerFiles, Guid fileId, string fileName, CancellationToken token)
    {
        if (!await ownerFiles.WithId(fileId).AnyAsync(token))
            return FileRenameOutcome.NotFound;

        var taken = await ownerFiles
            .Named(fileName)
            .Where(file => file.Id != fileId)
            .AnyAsync(token);

        if (taken)
            return FileRenameOutcome.NameTaken;

        int rows;

        try
        {
            rows = await ownerFiles
                .WithId(fileId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(static file => file.FileName, fileName), token);
        }
        catch (DbException exception) when (exception.IsUniqueViolation())
        {
            // The name was taken between the check above and this update.
            return FileRenameOutcome.NameTaken;
        }

        return rows == 0 ? FileRenameOutcome.NotFound : FileRenameOutcome.Renamed;
    }

    private static async Task<DeleteOutcome> DeleteFile(IQueryable<FileAsset> files, CancellationToken token)
    {
        var rows = await files.ExecuteDeleteAsync(token);

        return rows == 0 ? DeleteOutcome.NotFound : DeleteOutcome.Deleted;
    }

    /// <summary>
    /// A name the case or act already has, ignoring case, makes the upload that file's next version.
    /// </summary>
    private async Task<UploadFileResult> StoreUpload(IQueryable<FileAsset> ownerFiles, Guid? caseId, Guid? actId, FileUpload upload, CancellationToken token)
    {
        var context = dbSession.Current;

        var fileVersionId = Guid.CreateVersion7();

        // The blob is written before the row it backs; a blob orphaned by a failed write or an unchanged upload is tolerated (SDD-012).
        var blob = await blobStore.WriteFileBlob(userContext.TenantId, fileVersionId, upload.Content, token);

        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            var existingFileId = await ownerFiles
                .Named(upload.FileName)
                .Select(static file => (Guid?)file.Id)
                .SingleOrDefaultAsync(token);

            try
            {
                if (existingFileId is null)
                {
                    var file = new FileAsset { CaseId = caseId, ActId = actId, FileName = upload.FileName };

                    context.FileAssets.Add(file);
                    context.FileVersions.Add(NewVersion(file.Id, 1, fileVersionId, upload.MediaType, blob));
                    await context.SaveChangesAsync(token);

                    return new UploadFileResult { Outcome = UploadFileOutcome.Uploaded, File = await this.ReadListItem(file.Id, token) };
                }

                var outcome = await this.SaveNextVersion(existingFileId.Value, fileVersionId, upload.MediaType, blob, token);
                if (outcome == FileVersionUploadOutcome.FileNotFound)
                    continue; // deleted since it was found; the next attempt stores a new file

                var uploadOutcome = outcome switch
                {
                    FileVersionUploadOutcome.VersionAdded => UploadFileOutcome.VersionAdded,
                    FileVersionUploadOutcome.Unchanged => UploadFileOutcome.Unchanged,
                    FileVersionUploadOutcome.FileNotFound or FileVersionUploadOutcome.ExtensionMismatch => throw new UnreachableException(),
                    _ => throw new UnreachableException(),
                };

                return new UploadFileResult { Outcome = uploadOutcome, File = await this.ReadListItem(existingFileId.Value, token) };
            }
            catch (DbUpdateException exception) when (attempt < Attempts && exception.IsUniqueViolation())
            {
                this.ForgetLostRace();
            }
        }

        throw new InvalidOperationException("The upload lost every race for its file name.");
    }

    /// <summary>
    /// Saves the blob as the file's next version, numbered after its highest; content equal to the current version saves nothing.
    /// </summary>
    private async Task<FileVersionUploadOutcome> SaveNextVersion(Guid fileId, Guid fileVersionId, string? mediaType, FileBlobInfo blob, CancellationToken token)
    {
        var context = dbSession.Current;

        var current = await context.FileVersions.ReadFileVersion(fileId, version: null, token);
        if (current is null)
            return FileVersionUploadOutcome.FileNotFound;

        if (string.Equals(current.ContentHash, blob.ContentHash, StringComparison.Ordinal))
        {
            logger.LogInformation("An upload onto file {FileAssetId} matched its current version {FileVersionNumber} and stored no version", fileId, current.Number);

            return FileVersionUploadOutcome.Unchanged;
        }

        var version = NewVersion(fileId, current.Number + 1, fileVersionId, mediaType, blob);

        context.FileVersions.Add(version);
        await context.SaveChangesAsync(token);

        logger.LogInformation("File version {FileVersionNumber} of {FileAssetId} was stored, {SizeBytes} bytes", version.Number, fileId, version.SizeBytes);

        return FileVersionUploadOutcome.VersionAdded;
    }

    private void ForgetLostRace()
    {
        dbSession.Current.ChangeTracker.Clear();

        logger.LogWarning("A concurrent upload took the file name or the version number; the upload retries");
    }

    private async Task<FileListItem> ReadListItem(Guid fileId, CancellationToken token)
    {
        return await dbSession.Current.FileAssets
            .WithId(fileId)
            .AsListItems()
            .SingleAsync(token);
    }
}
