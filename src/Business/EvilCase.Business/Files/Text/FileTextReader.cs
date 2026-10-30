using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Files;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Files.Text;

internal sealed class FileTextReader(IDbSession dbSession, IFileBlobStore blobStore, ILogger<FileTextReader> logger) : IFileTextReader
{
    public async Task<FileTextResult> ReadFileText(Guid fileId, int? version, int page, CancellationToken token)
    {
        var fileVersion = await dbSession.Current.FileVersions.ReadFileVersion(fileId, version, token);
        if (fileVersion is null)
            return new FileTextResult { Outcome = FileTextOutcome.FileNotFound };

        var fileName = fileVersion.FileAsset!.FileName;

        if (!FileTextExtraction.Reads(fileName))
            return new FileTextResult { Outcome = FileTextOutcome.UnsupportedFormat };

        if (fileVersion.SizeBytes > FileLimits.MaxTextFileBytes)
            return new FileTextResult { Outcome = FileTextOutcome.TooLarge };

        await using var content = blobStore.ReadFileBlob(fileVersion.StoragePath);
        if (content is null)
            return new FileTextResult { Outcome = FileTextOutcome.FileNotFound };

        // The parsers read synchronously and seek; the blob is read off the disk asynchronously first.
        await using var buffer = new MemoryStream(capacity: (int)fileVersion.SizeBytes);
        await content.CopyToAsync(buffer, token);
        buffer.Position = 0;

        string text;

        try
        {
            text = FileTextExtraction.Extract(fileName, buffer);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Text of version {FileVersionNumber} of {FileAssetId} could not be read", fileVersion.Number, fileId);

            return new FileTextResult { Outcome = FileTextOutcome.Unreadable };
        }

        if (string.IsNullOrWhiteSpace(text))
            return new FileTextResult { Outcome = FileTextOutcome.NoText };

        var pageCount = FileTextPaging.PageCount(text.Length);

        if (FileTextPaging.Page(text, page) is not { } pageText)
            return new FileTextResult { Outcome = FileTextOutcome.PageNotFound, PageCount = pageCount };

        return new FileTextResult
        {
            Outcome = FileTextOutcome.Read,
            Page = new FileTextPage
            {
                FileId = fileId,
                FileName = fileName,
                Version = fileVersion.Number,
                Page = page,
                PageCount = pageCount,
                TotalLength = text.Length,
                Text = pageText,
            },
        };
    }
}
