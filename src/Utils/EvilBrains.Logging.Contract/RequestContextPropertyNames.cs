namespace EvilBrains.Logging.Contract;

/// <summary>
/// The X keeps XRequestId clear of RequestId, which ASP.NET Core fills with the trace identifier.
/// </summary>
public static class RequestContextPropertyNames
{
    public const string RequestId = "XRequestId";

    public const string CorrelationId = "XCorrelationId";

    public const string SessionId = "XSessionId";

    public const string MachineId = "XMachineId";
}
