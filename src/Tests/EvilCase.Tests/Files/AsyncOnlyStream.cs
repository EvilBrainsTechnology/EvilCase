namespace EvilBrains.EvilCase.Tests.Files;

/// <summary>
/// A response body as Kestrel gives it: forward only, and a synchronous write or flush throws.
/// </summary>
internal sealed class AsyncOnlyStream : Stream
{
    private readonly MemoryStream written = new();

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public byte[] Written()
    {
        return this.written.ToArray();
    }

    public override void Flush()
    {
        throw new InvalidOperationException("Synchronous operations are disallowed.");
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new InvalidOperationException("Synchronous operations are disallowed.");
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await this.written.WriteAsync(buffer, cancellationToken);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await this.written.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
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
            this.written.Dispose();

        base.Dispose(disposing);
    }
}
