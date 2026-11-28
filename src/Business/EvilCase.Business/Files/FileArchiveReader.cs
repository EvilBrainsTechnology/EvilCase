using System.IO.Compression;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Files;

internal sealed class FileArchiveReader(IDbSession dbSession, IFileBlobStore blobStore, ILogger<FileArchiveReader> logger) : IFileArchiveReader
{
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
            ActId = actId,
            FileName = FileArchiveLayout.ArchiveName(title, includeVersions),
            Entries = FileArchiveLayout.Entries(versions, includeVersions),
        };
    }

    public async Task WriteFileArchive(FileArchive archive, Stream target, CancellationToken token)
    {
        await using var zip = await ZipStreamWriter.Create(target, token);

        foreach (var entry in archive.Entries)
        {
            await using var content = blobStore.ReadFileBlob(entry.StoragePath)
                ?? throw new IOException($"The blob at {entry.StoragePath} is missing.");

            // Stored documents are mostly compressed already; the fastest level still shrinks text and XML.
            await zip.WriteEntry(entry.EntryName, entry.Created, CompressionLevel.Fastest, async entryStream => await content.CopyToAsync(entryStream, token), token);
        }

        await zip.Complete(token);

        logger.LogInformation("File archive of {ActId} was written, {EntryCount} entries", archive.ActId, archive.Entries.Count);
    }
}
