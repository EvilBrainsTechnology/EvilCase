using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Files;

internal sealed class FileBlobStore(IOptions<FileSettings> settings, ILogger<FileBlobStore> logger) : IFileBlobStore
{
    private const int BufferSize = 80 * 1024;

    // Resolved against the app directory, not the working directory, and normalised to one
    // trailing separator for the prefix check in FullPath.
    private readonly string rootFullPath =
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(settings.Value.RootPath, AppContext.BaseDirectory)) + Path.DirectorySeparatorChar;

    public async Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileAssetId, Stream content, CancellationToken token)
    {
        var storagePath = FileBlobPath.For(tenantId, fileAssetId);
        var fullPath = this.FullPath(storagePath);

        var temporaryPath = fullPath + ".tmp";

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var hasher = SHA256.Create();

        try
        {
            // The temp file sits in the target directory, so the rename below stays on one filesystem.
            await using (var target = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
            {
                await using var crypto = new CryptoStream(target, hasher, CryptoStreamMode.Write);
                await content.CopyToAsync(crypto, token);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch
        {
            // A failed cleanup must not replace the failure that got us here.
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

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

    public Stream? ReadFileBlob(string storagePath)
    {
        var fullPath = this.FullPath(storagePath);

        if (!File.Exists(fullPath))
        {
            logger.LogWarning("Blob at {StoragePath} is missing", storagePath);

            return null;
        }

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    public async Task DeleteFileBlob(string storagePath, CancellationToken token)
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
