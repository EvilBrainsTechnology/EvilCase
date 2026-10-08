using EvilBrains.EvilCase.App.Clipboard;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// Records what a render test copies; NSubstitute cannot substitute this internal interface.
/// </summary>
internal sealed class StubClipboardWriter : IClipboardWriter
{
    public List<string> Written { get; } = [];

    public Exception? Failure { get; init; }

    public Task WriteText(string text, CancellationToken token)
    {
        if (this.Failure is not null)
            return Task.FromException(this.Failure);

        this.Written.Add(text);

        return Task.CompletedTask;
    }
}
