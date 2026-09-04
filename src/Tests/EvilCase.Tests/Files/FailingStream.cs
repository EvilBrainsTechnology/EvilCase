namespace EvilBrains.EvilCase.Tests.Files;

internal sealed class FailingStream(byte[] head) : Stream
{
    private bool headReturned;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (this.headReturned)
            throw new IOException("The upload was cut short");

        this.headReturned = true;
        Array.Copy(head, 0, buffer, offset, head.Length);

        return head.Length;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    { }
}
