using System.IO.Compression;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Files;

internal sealed class FileArchiveReader(IDbSession dbSession, IFileBlobStore blobStore, ILogger<FileArchiveReader> logger) : IFileArchiveReader
{
    public async Task<FileArchive?> ReadCaseFileArchive(Guid caseId, bool includeVersions, CancellationToken token)
    {
        var context = dbSession.Current;

        var title = await context.Cases
            .WithId(caseId)
            .Select(static @case => @case.Title)
            .SingleOrDefaultAsync(token);

        if (title is null)
            return null;

        var versions = await context.FileVersions
            .OfCaseFiles(caseId)
            .InFileOrder()
            .AsArchivedVersions()
            .ToListAsync(token);

        return new FileArchive
        {
            OwnerId = caseId,
            FileName = FileArchiveLayout.ArchiveName(title, "Soubory spisu", includeVersions),
            Entries = FileArchiveLayout.Entries(versions, includeVersions),
        };
    }

    public async Task<FileArchive?> ReadActFileArchive(Guid actId, bool includeVersions, CancellationToken token)
    {
        var context = dbSession.Current;

        var title = await context.Acts
            .WithId(actId)
            .Select(static act => act.Title)
            .SingleOrDefaultAsync(token);

        if (title is null)
            return null;

        var versions = await context.FileVersions
            .OfActFiles(actId)
            .InFileOrder()
            .AsArchivedVersions()
            .ToListAsync(token);

        return new FileArchive
        {
            OwnerId = actId,
            FileName = FileArchiveLayout.ArchiveName(title, "Soubory úkonu", includeVersions),
            Entries = FileArchiveLayout.Entries(versions, includeVersions),
        };
    }

    public async Task WriteFileArchive(FileArchive archive, Stream target, CancellationToken token)
    {
        await using var output = new DeferredSyncWriteStream(target);

        // Not disposed on a failure: disposing writes the central directory, which would make a cut archive look whole.
        var zip = await ZipArchive.CreateAsync(output, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null, token);

        foreach (var entry in archive.Entries)
        {
            await using var content = blobStore.ReadFileBlob(entry.StoragePath)
                ?? throw new IOException($"The blob at {entry.StoragePath} is missing.");

            // Stored documents are mostly compressed already; the fastest level still shrinks text and XML.
            var zipEntry = zip.CreateEntry(entry.EntryName, CompressionLevel.Fastest);
            zipEntry.LastWriteTime = entry.Created;

            await using (var entryStream = await zipEntry.OpenAsync(token))
                await content.CopyToAsync(entryStream, token);

            await output.FlushAsync(token);
        }

        await zip.DisposeAsync();
        await output.FlushAsync(token);

        logger.LogInformation("File archive of {OwnerId} was written, {EntryCount} entries", archive.OwnerId, archive.Entries.Count);
    }
}
