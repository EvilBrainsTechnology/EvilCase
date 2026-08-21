using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Files;

internal sealed class FileBlobStore(IOptions<FileSettings> settings, ILogger<FileBlobStore> logger) : IFileBlobStore
{
    private const int BufferSize = 80 * 1024;

    // A rooted path is returned unchanged; a relative one resolves against the application directory
    // rather than the working directory, so it means the same thing wherever the process starts from.
    // Exactly one trailing separator, whatever the configured root ended with: the containment check
    // below compares this as a prefix, and a root written with a trailing slash would match nothing.
    private readonly string rootFullPath =
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(settings.Value.RootPath, AppContext.BaseDirectory)) + Path.DirectorySeparatorChar;

    public async Task<FileBlobInfo> Write(Guid tenantId, Guid fileAssetId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var storagePath = FileBlobPath.For(tenantId, fileAssetId, fileName);
        var fullPath = this.FullPath(storagePath);

        // A stored blob may itself be named "{fileAssetId}.tmp" — the kept extension allows it — so the
        // temporary file carries a suffix no stored name can have; otherwise a write for the same asset
        // would truncate that blob and a failure would delete it.
        var temporaryPath = fullPath + ".~tmp";

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var hasher = SHA256.Create();

        try
        {
            // The temp file sits in the target directory, so the rename below stays on one filesystem.
            await using (var target = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
            {
                await using var crypto = new CryptoStream(target, hasher, CryptoStreamMode.Write);
                await content.CopyToAsync(crypto, cancellationToken);
            }

            // The rename is the commit: until it runs, no reader sees a blob.
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch
        {
            // An upload that drops leaves the temp file behind, and nothing else ever removes it.
            File.Delete(temporaryPath);
            throw;
        }

        var info = new FileBlobInfo
        {
            StoragePath = storagePath,
            ContentHash = Convert.ToHexStringLower(hasher.Hash!),
            SizeBytes = new FileInfo(fullPath).Length,
        };

        logger.LogInformation("Blob at {StoragePath} was written, {SizeBytes} bytes", info.StoragePath, info.SizeBytes);

        return info;
    }

    public Task Delete(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = this.FullPath(storagePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogInformation("Blob at {StoragePath} was deleted", storagePath);
        }
        else
        {
            logger.LogWarning("Blob at {StoragePath} was already gone", storagePath);
        }

        return Task.CompletedTask;
    }

    private string FullPath(string storagePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(this.rootFullPath, storagePath.Replace('/', Path.DirectorySeparatorChar)));

        // The path comes back from the database; one that leaves the root is refused rather than followed.
        if (!fullPath.StartsWith(this.rootFullPath, StringComparison.Ordinal))
            throw new ArgumentException("The storage path leaves the storage root.", nameof(storagePath));

        return fullPath;
    }
}
