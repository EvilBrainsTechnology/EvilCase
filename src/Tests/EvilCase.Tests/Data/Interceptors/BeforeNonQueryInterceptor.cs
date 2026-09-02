using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvilBrains.EvilCase.Tests.Data.Interceptors;

/// <summary>
/// Runs an action once, just before the first statement that changes rows. It is what puts a write
/// between a read and the statement that acts on it without leaning on timing.
/// </summary>
internal sealed class BeforeNonQueryInterceptor(Action race) : DbCommandInterceptor
{
    private bool ran;

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (!this.ran)
        {
            this.ran = true;
            race();
        }

        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }
}
