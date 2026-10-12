namespace EvilBrains.EvilCase.App.Clipboard;

internal interface IClipboardWriter
{
    public Task WriteText(string text, CancellationToken token);
}
