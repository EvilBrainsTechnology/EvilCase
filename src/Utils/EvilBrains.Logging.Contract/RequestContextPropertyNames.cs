namespace EvilBrains.Logging.Contract;

/// <summary>
/// The X prefix keeps the identifiers together when a log store sorts properties by name, and keeps
/// them clear of <c>RequestId</c>, which ASP.NET Core owns and fills with the trace identifier.
/// </summary>
public static class RequestContextPropertyNames
{
    public const string RequestId = "XRequestId";

    public const string CorrelationId = "XCorrelationId";

    public const string SessionId = "XSessionId";

    public const string MachineId = "XMachineId";
}
