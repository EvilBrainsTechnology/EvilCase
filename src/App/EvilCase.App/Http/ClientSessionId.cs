namespace EvilBrains.EvilCase.App.Http;

/// <summary>
/// One identifier per application load. It lives outside the handler because the HTTP handler chain
/// is recycled periodically and a handler-local value would silently change mid-session.
/// </summary>
internal sealed class ClientSessionId
{
    public string Value { get; } = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
}
