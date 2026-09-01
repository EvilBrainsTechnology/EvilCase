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

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        this.Committed = true;
    }

    public void Rollback()
    { }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    { }

    public void Dispose()
    { }

    public async ValueTask DisposeAsync()
    { }
}
