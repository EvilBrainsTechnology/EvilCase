using System.Collections.Frozen;
using EvilBrains.Logging.Contract;
using Serilog.Core;

namespace EvilBrains.Logging.AspNetCore;

/// <summary>
/// Property names owned by the server. A browser log entry must never shadow them, because
/// properties carried on the event win over enrichers.
/// </summary>
internal static class ReservedLogPropertyNames
{
    private static readonly FrozenSet<string> Names = FrozenSet.Create(
        StringComparer.Ordinal,
        Constants.SourceContextPropertyName,
        RequestContextPropertyNames.RequestId,
        RequestContextPropertyNames.CorrelationId,
        RequestContextPropertyNames.SessionId,
        RequestContextPropertyNames.MachineId,
        "Application",
        "Environment",
        "MachineName",
        "ThreadId",
        "EventId",
        "ClientTimestamp",
        "ClientCategory",
        "ClientUrl",
        "ClientMessage",
        "RequestMethod",
        "RequestPath",
        "StatusCode",
        "Elapsed");

    public static bool Contains(string name) => Names.Contains(name);
}
