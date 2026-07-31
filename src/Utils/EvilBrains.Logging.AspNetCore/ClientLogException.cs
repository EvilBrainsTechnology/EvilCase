namespace EvilBrains.Logging.AspNetCore;

/// <summary>
/// Carries a browser-side exception; the client cannot send an exception object, only its rendered text.
/// </summary>
internal sealed class ClientLogException : Exception
{
    public ClientLogException()
    { }

    public ClientLogException(string message)
        : base(message)
    { }

    public ClientLogException(string message, Exception innerException)
        : base(message, innerException)
    { }

    public override string ToString() => this.Message;
}
