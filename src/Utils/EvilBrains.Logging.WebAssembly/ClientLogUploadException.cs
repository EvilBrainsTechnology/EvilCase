namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// The failures an uploader is expected to produce. Anything else is a defect and is not swallowed.
/// </summary>
public sealed class ClientLogUploadException : Exception
{
    public ClientLogUploadException()
    { }

    public ClientLogUploadException(string message)
        : base(message)
    { }

    public ClientLogUploadException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
