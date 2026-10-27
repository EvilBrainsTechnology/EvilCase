namespace EvilBrains.EvilCase.Tests.Files.Signatures;

/// <summary>
/// A stream whose content is read only once the gate opens; <paramref name="reading"/> completes when a read waits on it.
/// </summary>
internal sealed class GatedStream(byte[] content, Task gate, TaskCompletionSource reading) : MemoryStream(content, writable: false)
{
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        reading.TrySetResult();
        await gate.WaitAsync(cancellationToken);

        return await base.ReadAsync(buffer, cancellationToken);
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        reading.TrySetResult();
        await gate.WaitAsync(cancellationToken);

        return await base.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
    }
}
