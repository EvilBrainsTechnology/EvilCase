using System.IO.Compression;

namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// A zip archive streamed to its target entry by entry, writing only asynchronously and holding no more than tens of
/// kilobytes. Only <see cref="Complete"/> writes the archive's end, so an archive dropped on a failure never reads as
/// whole.
/// </summary>
internal sealed class ZipStreamWriter : IAsyncDisposable
{
    private readonly DeferredSyncWriteStream output;

    private readonly ZipArchive zip;

    private ZipStreamWriter(DeferredSyncWriteStream output, ZipArchive zip)
    {
        this.output = output;
        this.zip = zip;
    }

    public long BytesWritten => this.output.BytesWritten;

    public static async Task<ZipStreamWriter> Create(Stream target, CancellationToken token)
    {
        var stream = new DeferredSyncWriteStream(target);
        var archive = await ZipArchive.CreateAsync(stream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null, token);

        return new ZipStreamWriter(stream, archive);
    }

    /// <summary>
    /// The entry's time carries no zone; the caller gives it in the zone the archive is read in.
    /// </summary>
    public async Task WriteEntry(string entryName, DateTimeOffset lastWriteTime, CompressionLevel level, Func<Stream, Task> write, CancellationToken token)
    {
        var entry = this.zip.CreateEntry(entryName, level);
        entry.LastWriteTime = lastWriteTime;

        await using (var entryStream = await entry.OpenAsync(token))
            await write(entryStream);

        await this.output.FlushAsync(token);
    }

    public async Task WriteFolder(string folderName, DateTimeOffset lastWriteTime, CancellationToken token)
    {
        await this.WriteEntry(folderName + "/", lastWriteTime, CompressionLevel.NoCompression, static _ => Task.CompletedTask, token);
    }

    public async Task Complete(CancellationToken token)
    {
        await this.zip.DisposeAsync();
        await this.output.FlushAsync(token);
    }

    /// <summary>
    /// Leaves the archive as it is: disposing it would write the central directory, which would make a cut archive look
    /// whole.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await this.output.DisposeAsync();
    }
}
