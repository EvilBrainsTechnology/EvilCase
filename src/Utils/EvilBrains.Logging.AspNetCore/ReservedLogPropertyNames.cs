using System.Collections.Frozen;
using EvilBrains.Logging.Contract;
using Serilog.Core;

namespace EvilBrains.Logging.AspNetCore;

/// <summary>
/// Property names the server attaches to these events itself, through enrichers, the log context or
/// this writer. A browser entry must never shadow them, because properties carried on an event win
/// over enrichers. Names that only appear on other events — the request logging ones, for instance —
/// are deliberately not listed: the browser logs its own HTTP calls and those carry the same names.
/// </summary>
internal static class ReservedLogPropertyNames
{
    private static readonly FrozenSet<string> Names = FrozenSet.Create(
        StringComparer.Ordinal,
        Constants.SourceContextPropertyName,
        AppSource.PropertyName,
        RequestContextPropertyNames.RequestId,
        RequestContextPropertyNames.CorrelationId,
        RequestContextPropertyNames.SessionId,
        RequestContextPropertyNames.MachineId,
        "Application",
        "Environment",
        "MachineName",
        "ThreadId",
        "ClientTimestamp",
        "ClientCategory",
        "ClientUrl",
        "ClientMessage");

    public static bool Contains(string name) => Names.Contains(name);
}
