using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Files;

/// <summary>
/// Blobs on local disk at <c>&lt;root&gt;/&lt;first two hex characters&gt;/&lt;hash&gt;</c>, so no
/// single directory holds every blob.
/// </summary>
internal sealed class LocalFileStore : IFileStore
{
    private readonly string root;

    public LocalFileStore(IOptions<FileStoreSettings> settings, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(environment);

        this.root = Path.GetFullPath(settings.Value.RootPath, environment.ContentRootPath);
    }

    public async Task<StoredFile> Store(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        _ = Directory.CreateDirectory(this.root);

        // Moved into place rather than written in place, so a reader never meets a half-written file.
        var pending = Path.Combine(this.root, $".pending-{Guid.NewGuid():N}");

        try
        {
            var (hash, size) = await WriteAndHash(content, pending, cancellationToken);
            var destination = this.PathFor(hash);

            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            if (File.Exists(destination))
                return new(hash, size, AlreadyPresent: true);

            // Two callers storing identical content race here; the loser finds the file in place.
            try
            {
                File.Move(pending, destination);
            }
            catch (IOException) when (File.Exists(destination))
            {
                return new(hash, size, AlreadyPresent: true);
            }

            return new(hash, size, AlreadyPresent: false);
        }
        finally
        {
            if (File.Exists(pending))
                File.Delete(pending);
        }
    }

    public Task<Stream?> Open(string contentHash, CancellationToken cancellationToken = default)
    {
        var path = this.PathFor(contentHash);

        Stream? content = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 64 * 1024, useAsync: true)
            : null;

        return Task.FromResult(content);
    }

    public Task<bool> Exists(string contentHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(this.PathFor(contentHash)));

    private static async Task<(string Hash, long Size)> WriteAndHash(Stream content, string destination, CancellationToken cancellationToken)
    {
        using var hasher = SHA256.Create();

        await using (var file = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 64 * 1024, useAsync: true))
        {
            await using var hashing = new CryptoStream(file, hasher, CryptoStreamMode.Write, leaveOpen: true);

            await content.CopyToAsync(hashing, cancellationToken);
        }

        return (Convert.ToHexStringLower(hasher.Hash!), new FileInfo(destination).Length);
    }

    private string PathFor(string contentHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        // Everything reaching here came from hashing content, so a non-hash is a caller's bug.
        if (contentHash.Length != 64 || !contentHash.All(char.IsAsciiHexDigitLower))
            throw new ArgumentException("A content hash is 64 lower-case hex characters.", nameof(contentHash));

        return Path.Combine(this.root, contentHash[..2], contentHash);
    }
}
