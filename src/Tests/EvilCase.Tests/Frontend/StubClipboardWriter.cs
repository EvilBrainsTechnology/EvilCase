using EvilBrains.EvilCase.App.Clipboard;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// Copies nothing; NSubstitute cannot substitute this internal interface.
/// </summary>
internal sealed class StubClipboardWriter : IClipboardWriter
{
    public Task WriteText(string text, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}
