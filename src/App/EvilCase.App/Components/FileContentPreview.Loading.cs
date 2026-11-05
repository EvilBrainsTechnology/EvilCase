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

    private CancellationTokenSource? loadToken;

    internal static async Task<byte[]> ReadBytes(Stream content, CancellationToken token)
    {
        using var bytes = new MemoryStream();

        await content.CopyToAsync(bytes, token);

        return bytes.ToArray();
    }

    // Cancels what is loading, so a stale load never overwrites a newer one.
    private async Task Show()
    {
        this.loadToken?.Cancel();

        var token = (this.loadToken = new CancellationTokenSource()).Token;

        this.loading = true;
        this.failed = false;
        this.unsupported = false;

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

        if ((preview == FilePreviewKind.DataMessage && this.SizeBytes > DataMessageZfo.MaxBytes)
            || (preview == FilePreviewKind.SignedDocument && this.SizeBytes > FileLimits.MaxReadBytes))
        {
            this.loading = false;
            this.unsupported = true;

            return;
        }

        try
        {
            // The browser requests what it shows natively itself, and the PDF library requests a PDF; the rest is
            // read here as bytes.
            if (this.Content is null && (preview == FilePreviewKind.Pdf || InlineFileTypes.MediaType(file.FileName) is not null))
            {
                await this.Session.EnsureFresh(token);

                if (!token.IsCancellationRequested)
                {
                    this.loading = false;
                    this.url = FileUrl.Content(file.FileId, this.Version);

                    // The browser keeps requesting ranges while the file is shown; the next file or the close ends it.
                    _ = this.Session.KeepFresh(token);
                }

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
                await this.ShowUrl(await this.BrowserFiles.CreateBlobUrl(mediaType, content, token), token);
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
        switch (preview)
        {
            case FilePreviewKind.Text:
            case FilePreviewKind.Code:
            {
                var (text, cut, editable) = await ReadText(content, token);

                if (!token.IsCancellationRequested)
                {
                    (this.text, this.truncated) = (text, cut);
                    (this.editable, this.notUtf8) = (editable, !cut && editable is null);
                }

                break;
            }

            case FilePreviewKind.Markdown:
            {
                var (text, cut, editable) = await ReadText(content, token);

                if (!token.IsCancellationRequested)
                {
                    (this.markdown, this.truncated) = (text, cut);
                    (this.editable, this.notUtf8) = (editable, !cut && editable is null);
                }

                break;
            }

            // Only SVG is a picture read as bytes: it is shown as an image blob, never as the stored type.
            case FilePreviewKind.Image:
                await this.ShowUrl(await this.BrowserFiles.CreateBlobUrl("image/svg+xml", content, token), token);

                break;

            case FilePreviewKind.ConvertedImage:
                await this.ShowUrl(await this.Previews.CreatePngUrl(content, token), token);

                break;

            case FilePreviewKind.Document:
            {
                var bytes = await ReadBytes(content, token);

                if (!token.IsCancellationRequested)
                    this.document = bytes;

                break;
            }

            case FilePreviewKind.Workbook:
            {
                var sheets = await this.Previews.ReadWorkbook(content, MaxSheetRows, token);

                if (!token.IsCancellationRequested)
                    this.workbook = sheets;

                break;
            }

            case FilePreviewKind.Email:
            {
                var message = await this.Previews.ReadEmail(fileName, content, token);

                if (!token.IsCancellationRequested)
                    this.email = message;

                break;
            }

            // A stored PDF is requested by the library itself; only an attachment held in memory comes here.
            case FilePreviewKind.Pdf:
            {
                var bytes = await ReadBytes(content, token);

                if (!token.IsCancellationRequested)
                    this.pdf = bytes;

                break;
            }

            case FilePreviewKind.DataMessage:
            {
                var bytes = await ReadBytes(content, token);

                if (!token.IsCancellationRequested)
                    this.dataMessage = bytes;

                break;
            }

            case FilePreviewKind.SignedDocument:
            {
                var bytes = await ReadBytes(content, token);

                if (token.IsCancellationRequested)
                    break;

                var signed = SignedData.ParseOrDefault(bytes);

                if (signed is null)
                {
                    this.unsupported = true;

                    break;
                }

                // A detached signature carries no document; the stage shows its signers instead.
                if (signed.Content is not { } document)
                    break;

                var documentName = FileTypeDisplay.SignedContentName(fileName);

                // A name without a known extension takes the one of the type its content shows.
                if (!FileTypeDisplay.HasKnownExtension(documentName)
                    && await this.DetectExtension(document, token) is { } detected && FileTypeDisplay.IsKnownExtension(detected))
                    documentName = $"{documentName}.{detected}";

                if (token.IsCancellationRequested)
                    break;

                (this.signedContent, this.signedContentName) = (document, documentName);

                var documentPreview = FileTypeDisplay.Preview(documentName);

                await using var documentContent = new MemoryStream(document, writable: false);

                // A signature or data message inside the signature would need a stored file of its own.
                if (documentPreview is FilePreviewKind.SignedDocument or FilePreviewKind.DataMessage)
                    this.unsupported = true;
                else if (InlineFileTypes.MediaType(documentName) is { } mediaType)
                    await this.ShowUrl(await this.BrowserFiles.CreateBlobUrl(mediaType, documentContent, token), token);
                else if (documentPreview != FilePreviewKind.None)
                    await this.Read(documentName, documentPreview, documentContent, token);

                break;
            }

            case FilePreviewKind.None:
            case FilePreviewKind.Audio:
            case FilePreviewKind.Video:
                throw new UnreachableException();

            default:
                throw new UnreachableException();
        }
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
        return preview is FilePreviewKind.ConvertedImage or FilePreviewKind.Document or FilePreviewKind.Workbook or FilePreviewKind.Email
            or FilePreviewKind.SignedDocument;
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

        var truncated = read > FileTypeDisplay.MaxTextBytes;
        var shown = buffer.AsSpan(0, Math.Min(read, FileTypeDisplay.MaxTextBytes));
        var editable = truncated ? null : EditableText.ParseOrDefault(shown);

        return (editable?.Text ?? Encoding.UTF8.GetString(shown).TrimStart('﻿'), truncated, editable);
    }
}
