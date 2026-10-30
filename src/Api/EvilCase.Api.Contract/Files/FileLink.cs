namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// A one-time link that uploads or downloads the bytes of a file outside the MCP connection.
/// </summary>
public sealed record FileLink
{
    public required string Method { get; init; }

    /// <summary>
    /// Absolute; it carries the secret, so it is a credential until used or expired.
    /// </summary>
    public required string Url { get; init; }

    public required DateTime Expires { get; init; }

    public required string Curl { get; init; }
}
