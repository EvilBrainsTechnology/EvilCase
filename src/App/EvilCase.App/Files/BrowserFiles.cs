using EvilBrains.EvilCase.App.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class BrowserFiles(IJSRuntime jsRuntime, IAuthSession session) : JsModule(jsRuntime, "./js/files.js"), IBrowserFiles
{
    public async Task DownloadFile(Guid fileId, int? version, string fileName, CancellationToken token)
    {
        await session.EnsureFresh(token);

        await this.InvokeVoid("downloadFile", token, FileUrl.Content(fileId, version), fileName);
    }

    public async Task DownloadArchive(string archiveUrl, CancellationToken token)
    {
        await session.EnsureFresh(token);

        await this.InvokeVoid("downloadFile", token, archiveUrl, "");
    }

    public async Task DownloadContent(string fileName, Stream content, CancellationToken token)
    {
        using var streamRef = new DotNetStreamReference(content);

        await this.InvokeVoid("downloadContent", token, fileName, streamRef);
    }

    public async Task<string> CreateBlobUrl(string mediaType, Stream content, CancellationToken token)
    {
        var files = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        // Never cancelled once started: a URL the browser made after a cancel must reach the caller to be revoked.
        return await files.InvokeAsync<string>("createObjectUrl", CancellationToken.None, mediaType, streamRef);
    }

    public async Task RevokeBlobUrl(string url)
    {
        await this.InvokeVoid("revokeObjectUrl", url);
    }

    public async Task PickFile(ElementReference input, CancellationToken token)
    {
        await this.InvokeVoid("pickFile", token, input);
    }

    public async Task AttachDropZone(ElementReference card, CancellationToken token)
    {
        await this.InvokeVoid("attachDropZone", token, card);
    }
}
