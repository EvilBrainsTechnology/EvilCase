using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// A real transaction that also remembers whether it was committed. The writes under it share one
/// <c>now()</c>, which is what a cascade's single stamp rests on.
/// </summary>
internal sealed class RecordedDbContextTransaction(IDbContextTransaction transaction) : IDbContextTransaction
{
    public Guid TransactionId => transaction.TransactionId;

    public bool Committed { get; private set; }

    public void Commit()
    {
        transaction.Commit();
        this.Committed = true;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await transaction.CommitAsync(cancellationToken);
        this.Committed = true;
    }

    public void Rollback()
    {
        transaction.Rollback();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await transaction.RollbackAsync(cancellationToken);
    }

    public void Dispose()
    {
        transaction.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await transaction.DisposeAsync();
    }
}
