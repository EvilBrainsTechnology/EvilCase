using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// Records whether anything was logged at or above <see cref="LogLevel.Warning"/>. NSubstitute
/// cannot match <see cref="ILogger.Log{TState}"/>'s generic <c>TState</c> reliably, so a plain
/// recorder stands in instead.
/// </summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public bool LoggedWarningOrAbove { get; private set; }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel >= LogLevel.Warning)
            this.LoggedWarningOrAbove = true;
    }
}
