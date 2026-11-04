using System.Security.Cryptography;
using System.Text;
using EvilBrains.EvilCase.Files;

namespace EvilBrains.EvilCase.Tests.Files;

/// <summary>
/// Holds every write until all parties arrive, so concurrent uploads reach the database together.
/// </summary>
internal sealed class MeetingBlobStore(int parties) : IFileBlobStore
{
    private readonly TaskCompletionSource meeting = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int arrived;

    public async Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileVersionId, Stream content, CancellationToken token)
    {
        using var reader = new StreamReader(content, Encoding.UTF8);
        var bytes = Encoding.UTF8.GetBytes(await reader.ReadToEndAsync(token));

        if (Interlocked.Increment(ref this.arrived) == parties)
            this.meeting.SetResult();

        await this.meeting.Task;

        return new FileBlobInfo
        {
            StoragePath = $"{tenantId}/{fileVersionId}",
            ContentHash = Convert.ToHexStringLower(SHA256.HashData(bytes)),
            SizeBytes = bytes.Length,
        };
    }

    public Stream? ReadFileBlob(string storagePath)
    {
        return null;
    }
}
