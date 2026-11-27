using EvilBrains.EvilCase.Files;

namespace EvilBrains.EvilCase.Business.Files;

internal static class FileBlobStoreExtensions
{
    /// <summary>
    /// The whole blob in memory, for a reader that parses synchronously; null where the blob is missing.
    /// </summary>
    public static async Task<byte[]?> ReadFileBlobBytes(this IFileBlobStore blobStore, string storagePath, long sizeBytes, CancellationToken token)
    {
        await using var content = blobStore.ReadFileBlob(storagePath);
        if (content is null)
            return null;

        await using var buffer = new MemoryStream(capacity: (int)sizeBytes);
        await content.CopyToAsync(buffer, token);

        // A blob of its recorded size fills the buffer exactly, so it is returned without a copy.
        return buffer.Length == buffer.Capacity ? buffer.GetBuffer() : buffer.ToArray();
    }
}
