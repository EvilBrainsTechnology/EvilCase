using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Tests.Data;

internal sealed class FakeDbContextTransaction : IDbContextTransaction
{
    public Guid TransactionId { get; } = Guid.CreateVersion7();

    public bool Committed { get; private set; }

    public void Commit()
    {
        this.Committed = true;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        this.Committed = true;
        return Task.CompletedTask;
    }

    public void Rollback()
    { }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    { }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
