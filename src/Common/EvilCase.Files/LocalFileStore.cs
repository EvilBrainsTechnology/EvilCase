using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Files;

/// <summary>
/// Blobs on local disk at <c>&lt;root&gt;/&lt;first two hex characters&gt;/&lt;hash&gt;</c>.
/// </summary>
/// <remarks>
/// The two-character directory is not decoration: one real case file is around three hundred documents,
/// and a flat directory holding every blob an owner ever stored is one nobody wants to list.
/// </remarks>
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

        // Written under a name nothing reads, then moved into place: a hash is a promise about the bytes
        // behind it, and a reader must never meet a half-written file keeping that promise badly.
        var pending = Path.Combine(this.root, $".pending-{Guid.NewGuid():N}");

        try
        {
            var (hash, size) = await WriteAndHash(content, pending, cancellationToken);
            var destination = this.PathFor(hash);

            _ = Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            if (File.Exists(destination))
                return new(hash, size, AlreadyPresent: true);

            // Two callers storing identical content race here; the loser finds the file already in place,
            // and both are right about what is stored.
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

        // Rejected rather than sanitised: everything reaching here came from hashing content, so a value
        // that is not a hash means a caller lost track of what it was holding.
        if (contentHash.Length != 64 || !contentHash.All(char.IsAsciiHexDigitLower))
            throw new ArgumentException("A content hash is 64 lower-case hex characters.", nameof(contentHash));

        return Path.Combine(this.root, contentHash[..2], contentHash);
    }
}
