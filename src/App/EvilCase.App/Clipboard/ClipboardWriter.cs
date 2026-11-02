using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Clipboard;

internal sealed class ClipboardWriter(IJSRuntime jsRuntime) : JsModule(jsRuntime, "./js/clipboard.js"), IClipboardWriter
{
    public async Task WriteText(string text, CancellationToken token)
    {
        await this.InvokeVoid("writeText", token, text);
    }
}
