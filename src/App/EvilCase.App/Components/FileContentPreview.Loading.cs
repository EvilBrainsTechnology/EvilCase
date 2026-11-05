using System.Diagnostics;
using System.Text;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Components;

public partial class FileContentPreview
{
    private const int MaxSheetRows = 1000;

    private static readonly string[] WorkbookAssemblies = ["ExcelDataReader.wasm", "ExcelNumberFormat.wasm", "System.Text.Encoding.CodePages.wasm"];

    private CancellationTokenSource? loadToken;

    internal static async Task<byte[]> ReadBytes(Stream content, CancellationToken token)
    {
        await using var bytes = new MemoryStream();

        await content.CopyToAsync(bytes, token);

        return bytes.ToArray();
    }

    // Cancels what is loading, so a stale load never overwrites a newer one.
    private async Task Show()
    {
        var previous = this.loadToken;
        var token = (this.loadToken = new CancellationTokenSource()).Token;

        this.loading = true;
        this.failed = false;
        this.unsupported = false;

        if (previous is not null)
            await previous.CancelAsync();

        await this.ReleaseUrl();

        if (this.Attachment is { } attachment)
            await this.ShowAttachment(attachment, token);
        else
            await this.ShowStored(token);

        if (!token.IsCancellationRequested)
            await this.OnChanged.InvokeAsync(this);
    }

    private async Task ShowStored(CancellationToken token)
    {
        var file = this.File;
        var preview = FileTypeDisplay.Preview(file.FileName);

        if (preview == FilePreviewKind.None)
        {
            this.loading = false;

            return;
        }

        if (this.OverReadLimit(preview))
        {
            this.loading = false;
            this.unsupported = true;

            return;
        }

        try
        {
            // The browser and the PDF library request what they show themselves; the rest is read here as bytes.
            if (this.Content is null && (preview == FilePreviewKind.Pdf || InlineFileTypes.MediaType(file.FileName) is not null))
            {
                await this.ShowStoredUrl(file, token);

                return;
            }

            await using var content = this.Content is { } read ? new MemoryStream(read, writable: false) : await this.LoadContent(file, this.Version, token);

            await this.Read(file.FileName, preview, content, token);

            if (!token.IsCancellationRequested)
                this.loading = false;
        }
        catch (OperationCanceledException)
        {
            // A later file took over.
        }
        catch (JSException exception) when (UsesLibrary(preview))
        {
            this.Logger.LogInformation(exception, "The library could not read file {FileId} for the preview", file.FileId);

            if (!token.IsCancellationRequested)
            {
                this.loading = false;
                this.unsupported = true;
            }
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or JSException)
        {
            this.Logger.LogWarning(exception, "Loading file {FileId} for the preview failed", file.FileId);

            if (!token.IsCancellationRequested)
            {
                this.loading = false;
                this.failed = true;
            }
        }
    }

    // A data message or a signed file is read whole, so one over its limit is not read.
    private bool OverReadLimit(FilePreviewKind preview)
    {
        return (preview == FilePreviewKind.DataMessage && this.SizeBytes > DataMessageZfo.MaxBytes)
            || (preview == FilePreviewKind.SignedDocument && this.SizeBytes > FileLimits.MaxReadBytes);
    }

    private async Task ShowStoredUrl(FileListItem file, CancellationToken token)
    {
        await this.Session.EnsureFresh(token);

        if (token.IsCancellationRequested)
            return;

        this.loading = false;
        this.url = FileUrl.Content(file.FileId, this.Version);

        // The browser keeps requesting ranges while the file is shown; the next file or the close ends it.
        _ = this.Session.KeepFresh(token);
    }

    private async Task ShowAttachment(DataMessageAttachment attachment, CancellationToken token)
    {
        var preview = FileTypeDisplay.Preview(attachment.Name);

        if (preview == FilePreviewKind.None)
        {
            this.loading = false;

            return;
        }

        // A signed file inside a data message has no stored file whose signers the server reads.
        if (preview == FilePreviewKind.SignedDocument)
        {
            this.loading = false;
            this.unsupported = true;

            return;
        }

        try
        {
            await using var content = new MemoryStream(attachment.Content, writable: false);

            // What the browser shows natively is a blob of the type the extension gives.
            if (InlineFileTypes.MediaType(attachment.Name) is { } mediaType)
                await this.ShowBlob(mediaType, content, token);
            else
                await this.Read(attachment.Name, preview, content, token);

            if (!token.IsCancellationRequested)
                this.loading = false;
        }
        catch (OperationCanceledException)
        {
            // A later attachment or file took over.
        }
        catch (JSException exception)
        {
            this.Logger.LogInformation(exception, "The library could not read an attachment of a data message for the preview");

            if (!token.IsCancellationRequested)
            {
                this.loading = false;
                this.unsupported = true;
            }
        }
    }

    // Each branch writes nothing once a later file took over, so a stale load never overwrites a newer one.
    private async Task Read(string fileName, FilePreviewKind preview, Stream content, CancellationToken token)
    {
        await (preview switch
        {
            FilePreviewKind.Text or FilePreviewKind.Code or FilePreviewKind.Markdown => this.ShowText(preview, content, token),

            // Only SVG is a picture read as bytes: it is shown as an image blob, never as the stored type.
            FilePreviewKind.Image => this.ShowBlob("image/svg+xml", content, token),
            FilePreviewKind.ConvertedImage => this.ShowPng(content, token),
            FilePreviewKind.Workbook => this.ReadWorkbook(content, token),
            FilePreviewKind.Email => this.ReadEmail(fileName, content, token),

            // A stored PDF is requested by the library itself; only an attachment held in memory comes here.
            FilePreviewKind.Document or FilePreviewKind.Pdf or FilePreviewKind.DataMessage => this.HoldBytes(preview, content, token),
            FilePreviewKind.SignedDocument => this.ReadSignedDocument(fileName, content, token),
            FilePreviewKind.None or FilePreviewKind.Audio or FilePreviewKind.Video => throw new UnreachableException(),
            _ => throw new UnreachableException(),
        });
    }

    private async Task ShowBlob(string mediaType, Stream content, CancellationToken token)
    {
        await this.ShowUrl(await this.BrowserFiles.CreateBlobUrl(mediaType, content, token), token);
    }

    private async Task ShowPng(Stream content, CancellationToken token)
    {
        await this.ShowUrl(await this.Previews.CreatePngUrl(content, token), token);
    }

    // The readers load on the first workbook; the reading touches their types only once they are there.
    private async Task ReadWorkbook(Stream content, CancellationToken token)
    {
        var bytes = await ReadBytes(content, token);

        await this.AssemblyLoader.LoadAssembliesAsync(WorkbookAssemblies);

        if (token.IsCancellationRequested)
            return;

        if (WorkbookReader.ReadOrDefault(bytes, MaxSheetRows) is { } sheets)
            this.workbook = sheets;
        else
            this.unsupported = true;
    }

    private async Task ReadEmail(string fileName, Stream content, CancellationToken token)
    {
        var message = await this.Previews.ReadEmail(fileName, content, token);

        if (!token.IsCancellationRequested)
            this.email = message;
    }

    private async Task HoldBytes(FilePreviewKind preview, Stream content, CancellationToken token)
    {
        var bytes = await ReadBytes(content, token);

        if (token.IsCancellationRequested)
            return;

        if (preview == FilePreviewKind.Document)
            this.document = bytes;
        else if (preview == FilePreviewKind.Pdf)
            this.pdf = bytes;
        else
            this.dataMessage = bytes;
    }

    private async Task ShowText(FilePreviewKind preview, Stream content, CancellationToken token)
    {
        var (text, cut, editable) = await ReadText(content, token);

        if (token.IsCancellationRequested)
            return;

        if (preview == FilePreviewKind.Markdown)
            this.markdown = text;
        else
            this.text = text;

        this.truncated = cut;
        (this.editable, this.notUtf8) = (editable, !cut && editable is null);
    }

    // A signed document shows what it carries, read by the type of its own name.
    private async Task ReadSignedDocument(string fileName, Stream content, CancellationToken token)
    {
        var bytes = await ReadBytes(content, token);

        if (token.IsCancellationRequested)
            return;

        var signed = SignedData.ParseOrDefault(bytes);

        if (signed is null)
        {
            this.unsupported = true;

            return;
        }

        // A detached signature carries no document; the stage shows its signers instead.
        if (signed.Content is not { } document)
            return;

        var documentName = FileTypeDisplay.SignedContentName(fileName);

        // A name without a known extension takes the one of the type its content shows.
        if (!FileTypeDisplay.HasKnownExtension(documentName)
            && await this.DetectExtension(document, token) is { } detected
            && FileTypeDisplay.IsKnownExtension(detected))
        {
            documentName = $"{documentName}.{detected}";
        }

        if (token.IsCancellationRequested)
            return;

        (this.signedContent, this.signedContentName) = (document, documentName);

        var documentPreview = FileTypeDisplay.Preview(documentName);

        await using var documentContent = new MemoryStream(document, writable: false);

        // A signature or data message inside the signature would need a stored file of its own.
        if (documentPreview is FilePreviewKind.SignedDocument or FilePreviewKind.DataMessage)
            this.unsupported = true;
        else if (InlineFileTypes.MediaType(documentName) is { } mediaType)
            await this.ShowBlob(mediaType, documentContent, token);
        else if (documentPreview != FilePreviewKind.None)
            await this.Read(documentName, documentPreview, documentContent, token);
    }

    // A library that fails leaves the type undetected.
    private async Task<string?> DetectExtension(byte[] content, CancellationToken token)
    {
        await using var stream = new MemoryStream(content, writable: false);

        try
        {
            return await this.Previews.DetectExtension(stream, token);
        }
        catch (JSException exception)
        {
            this.Logger.LogInformation(exception, "The library could not detect the type of a signed document");

            return null;
        }
    }

    private async Task ShowUrl(string created, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            await this.BrowserFiles.RevokeBlobUrl(created);
        else
            (this.url, this.urlIsBlob) = (created, true);
    }

    private async Task ReleaseUrl()
    {
        if (this.url is null)
            return;

        var released = this.url;
        this.url = null;

        if (this.urlIsBlob)
            await this.BrowserFiles.RevokeBlobUrl(released);

        this.urlIsBlob = false;
    }

    // A signed document's content is read by the library of its own type.
    private static bool UsesLibrary(FilePreviewKind preview)
    {
        return preview is FilePreviewKind.ConvertedImage or FilePreviewKind.Document or FilePreviewKind.Email or FilePreviewKind.SignedDocument;
    }

    // Only the start of a long file is read; the cut may split a character, which shows as one replacement mark. The text
    // is editable only when read whole and valid UTF-8.
    private static async Task<(string Text, bool Truncated, EditableText? Editable)> ReadText(Stream content, CancellationToken token)
    {
        var buffer = new byte[FileTypeDisplay.MaxTextBytes + 1];
        var read = 0;

        while (read < buffer.Length)
        {
            var count = await content.ReadAsync(buffer.AsMemory(read), token);

            if (count == 0)
                break;

            read += count;
        }

        var cut = read > FileTypeDisplay.MaxTextBytes;
        var shown = buffer.AsSpan(0, Math.Min(read, FileTypeDisplay.MaxTextBytes));
        var whole = cut ? null : EditableText.ParseOrDefault(shown);

        return (whole?.Text ?? Encoding.UTF8.GetString(shown).TrimStart('﻿'), cut, whole);
    }
}
