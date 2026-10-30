using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Tests.Data;

internal sealed class FakeDbContextTransaction : IDbContextTransaction
{
    public Guid TransactionId { get; } = Guid.CreateVersion7();

    public void Commit()
    { }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    { }

    public void Rollback()
    { }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    { }

    public void Dispose()
    { }

    public async ValueTask DisposeAsync()
    { }
}
