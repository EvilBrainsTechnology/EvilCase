namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// Holds what is written synchronously until the next asynchronous write or flush, so the target never sees a
/// synchronous write. A zip entry's stream writes its end synchronously, also when it is disposed asynchronously;
/// that end is a few kilobytes.
/// </summary>
internal sealed class DeferredSyncWriteStream(Stream target) : Stream
{
    private readonly MemoryStream deferred = new();

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        // The deferred bytes leave with the next asynchronous write or flush.
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await this.WriteDeferred(cancellationToken);
        await target.FlushAsync(cancellationToken);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        this.deferred.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        this.deferred.Write(buffer);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await this.WriteDeferred(cancellationToken);
        await target.WriteAsync(buffer, cancellationToken);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await this.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            this.deferred.Dispose();

        base.Dispose(disposing);
    }

    private async Task WriteDeferred(CancellationToken token)
    {
        if (this.deferred.Length == 0)
            return;

        await target.WriteAsync(this.deferred.GetBuffer().AsMemory(0, (int)this.deferred.Length), token);
        this.deferred.SetLength(0);
    }
}
