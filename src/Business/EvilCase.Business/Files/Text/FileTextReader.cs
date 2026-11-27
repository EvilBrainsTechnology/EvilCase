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
            return FileTextFailure.FileNotFound;

        var fileName = fileVersion.FileAsset!.FileName;

        if (!FileTextExtraction.Reads(fileName))
            return FileTextFailure.UnsupportedFormat;

        if (fileVersion.SizeBytes > FileLimits.MaxReadBytes)
            return FileTextFailure.TooLarge;

        // The parsers read synchronously and seek; the blob is read off the disk asynchronously first.
        var content = await blobStore.ReadFileBlobBytes(fileVersion.StoragePath, fileVersion.SizeBytes, token);
        if (content is null)
            return FileTextFailure.FileNotFound;

        await using var buffer = new MemoryStream(content, writable: false);

        string text;

        try
        {
            text = FileTextExtraction.Extract(fileName, buffer);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Text of version {FileVersionNumber} of {FileAssetId} could not be read", fileVersion.Number, fileId);

            return FileTextFailure.Unreadable;
        }

        if (string.IsNullOrWhiteSpace(text))
            return FileTextFailure.NoText;

        var pageCount = FileTextPaging.PageCount(text.Length);

        if (FileTextPaging.Page(text, page) is not { } pageText)
            return new FileTextPageNotFound { PageCount = pageCount };

        return new FileTextPage
        {
            FileId = fileId,
            FileName = fileName,
            Version = fileVersion.Number,
            Page = page,
            PageCount = pageCount,
            TotalLength = text.Length,
            Text = pageText,
        };
    }
}
