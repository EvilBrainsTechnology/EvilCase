using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Data.Files;

internal sealed class FileBlobStore(FileStorageSettings settings, ILogger<FileBlobStore> logger) : IFileBlobStore
{
    private const int CopyBufferSize = 81920;

    public async Task<FileBlobInfo> Write(Guid tenantId, Guid fileAssetId, Stream content, CancellationToken cancellationToken = default)
    {
        var directory = this.TenantDirectory(tenantId);
        var path = this.BlobPath(tenantId, fileAssetId);
        var temporaryPath = path + ".tmp";

        Directory.CreateDirectory(directory);

        using var hasher = SHA256.Create();

        try
        {
            // The temp file sits in the target directory, so the rename below stays on one filesystem.
            await using (var target = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, FileOptions.Asynchronous))
            {
                await using var crypto = new CryptoStream(target, hasher, CryptoStreamMode.Write);
                await content.CopyToAsync(crypto, cancellationToken);
            }

            // The rename is the commit: until it runs, no reader sees a blob.
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch
        {
            // An upload that drops leaves the temp file behind, and nothing else ever removes it.
            File.Delete(temporaryPath);
            throw;
        }

        var info = new FileBlobInfo(Convert.ToHexStringLower(hasher.Hash!), new FileInfo(path).Length);

        logger.LogInformation("Blob {FileAssetId} of tenant {TenantId} was written, {SizeBytes} bytes", fileAssetId, tenantId, info.SizeBytes);

        return info;
    }

    public bool Delete(Guid tenantId, Guid fileAssetId)
    {
        var path = this.BlobPath(tenantId, fileAssetId);
        var existed = File.Exists(path);

        if (existed)
        {
            File.Delete(path);
            logger.LogInformation("Blob {FileAssetId} of tenant {TenantId} was deleted", fileAssetId, tenantId);
        }
        else
        {
            logger.LogWarning("Blob {FileAssetId} of tenant {TenantId} was already gone", fileAssetId, tenantId);
        }

        return existed;
    }

    private string TenantDirectory(in Guid tenantId) => Path.Combine(settings.RootPath, tenantId.ToString("D", CultureInfo.InvariantCulture));

    private string BlobPath(in Guid tenantId, in Guid fileAssetId) => Path.Combine(this.TenantDirectory(tenantId), fileAssetId.ToString("D", CultureInfo.InvariantCulture));
}
