using System.Data.Common;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvilBrains.EvilCase.Data.Interceptors;

/// <summary>
/// Sets the session-local <c>evilcase.user_id</c> the history triggers read, on every connection
/// this context opens — including the ones behind <c>ExecuteUpdate</c> and <c>ExecuteDelete</c>.
/// </summary>
internal sealed class HistoryUserInterceptor(IUserContext userContext) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateCommand(connection, userContext.UserIdOrDefault);
        command.ExecuteNonQuery();

        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using (var command = CreateCommand(connection, userContext.UserIdOrDefault))
            await command.ExecuteNonQueryAsync(cancellationToken);

        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static DbCommand CreateCommand(DbConnection connection, Guid? userId)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('evilcase.user_id', $1, false)";

        var parameter = command.CreateParameter();
        parameter.Value = userId?.ToString() ?? "";
        command.Parameters.Add(parameter);

        return command;
    }
}
