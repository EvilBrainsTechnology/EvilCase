using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Export;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace EvilBrains.EvilCase.Api.Controllers;

/// <summary>
/// No [GenerateApiClient]: the generator cannot express multipart or a byte stream. The browser downloads by an
/// ordinary request, which the session cookie authenticates.
/// </summary>
[ApiController]
[Route("api")]
public class FileTransferController : ControllerBase
{
    [HttpPost("cases/{caseId:guid}/files")]
    [RequestSizeLimit(FileLimits.MaxUploadRequestBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<FileListItem>> UploadCaseFile([FromServices] IFileWriter writer, [FromRoute] Guid caseId, [FromForm] IFormFile file, CancellationToken token)
    {
        var upload = this.BuildFileUpload(file, out var error);
        if (error is not null)
            return error;

        await using var content = upload!.Content;

        var result = await writer.UploadCaseFile(caseId, upload, token);

        return result.Outcome switch
        {
            UploadFileOutcome.Uploaded or UploadFileOutcome.VersionAdded => this.CreatedAtAction(nameof(this.DownloadFileContent), new { fileId = result.File!.FileId }, result.File),
            UploadFileOutcome.Unchanged => this.Ok(result.File),
            UploadFileOutcome.OwnerNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: CaseProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPost("acts/{actId:guid}/files")]
    [RequestSizeLimit(FileLimits.MaxUploadRequestBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<FileListItem>> UploadActFile(
        [FromServices] IFileWriter writer, [FromRoute] Guid actId, [FromForm] IFormFile file, CancellationToken token)
    {
        var upload = this.BuildFileUpload(file, out var error);
        if (error is not null)
            return error;

        await using var content = upload!.Content;

        var result = await writer.UploadActFile(actId, upload, token);

        return result.Outcome switch
        {
            UploadFileOutcome.Uploaded or UploadFileOutcome.VersionAdded => this.CreatedAtAction(nameof(this.DownloadFileContent), new { fileId = result.File!.FileId }, result.File),
            UploadFileOutcome.Unchanged => this.Ok(result.File),
            UploadFileOutcome.OwnerNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPost("files/{fileId:guid}/versions")]
    [RequestSizeLimit(FileLimits.MaxUploadRequestBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<FileListItem>> UploadFileVersion(
        [FromServices] IFileWriter writer, [FromRoute] Guid fileId, [FromForm] IFormFile file, CancellationToken token)
    {
        var upload = this.BuildFileUpload(file, out var error);
        if (error is not null)
            return error;

        await using var content = upload!.Content;

        var result = await writer.UploadFileVersion(fileId, upload, token);

        return result.Outcome switch
        {
            FileVersionUploadOutcome.VersionAdded => this.CreatedAtAction(nameof(this.DownloadFileContent), new { fileId, version = result.File!.Version }, result.File),
            FileVersionUploadOutcome.Unchanged => this.Ok(result.File),
            FileVersionUploadOutcome.FileNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound),
            FileVersionUploadOutcome.ExtensionMismatch => this.ExtensionMismatchProblem(),
            _ => throw new UnreachableException(),
        };
    }

    [HttpGet("files/{fileId:guid}/content")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadFileContent([FromServices] IFileReader files, [FromRoute] Guid fileId, [FromQuery] int? version, CancellationToken token)
    {
        var download = await files.OpenFileContent(fileId, version, token);

        if (download is null)
            return this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound);

        // Only the types a browser shows natively are inline, under the type the extension gives; any other
        // stored document, PDF included, is an opaque attachment (SDD-012). nosniff comes from SecurityHeadersMiddleware.
        if (InlineFileTypes.MediaType(download.FileName) is not { } inlineMediaType)
            return this.File(download.Content, "application/octet-stream", download.FileName, enableRangeProcessing: true);

        var disposition = new ContentDispositionHeaderValue("inline");
        disposition.SetHttpFileName(download.FileName);
        this.Response.Headers.ContentDisposition = disposition.ToString();

        return this.File(download.Content, inlineMediaType, enableRangeProcessing: true);
    }

    [HttpGet("cases/{caseId:guid}/files/archive")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadCaseFileArchive(
        [FromServices] IFileArchiveReader archives,
        [FromServices] ILogger<FileTransferController> logger,
        [FromRoute] Guid caseId,
        [FromQuery] bool includeVersions,
        CancellationToken token)
    {
        var archive = await archives.ReadCaseFileArchive(caseId, includeVersions, token);

        if (archive is null)
            return this.Problem(statusCode: StatusCodes.Status404NotFound, title: CaseProblems.NotFound);

        await this.SendFileArchive(archives, logger, archive, token);

        return new EmptyResult();
    }

    [HttpGet("acts/{actId:guid}/files/archive")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadActFileArchive(
        [FromServices] IFileArchiveReader archives,
        [FromServices] ILogger<FileTransferController> logger,
        [FromRoute] Guid actId,
        [FromQuery] bool includeVersions,
        CancellationToken token)
    {
        var archive = await archives.ReadActFileArchive(actId, includeVersions, token);

        if (archive is null)
            return this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound);

        await this.SendFileArchive(archives, logger, archive, token);

        return new EmptyResult();
    }

    /// <summary>
    /// Everything the caller's tenant holds (SDD-022).
    /// </summary>
    [HttpGet("export")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DownloadTenantExport([FromServices] ITenantExporter exporter, [FromServices] ILogger<FileTransferController> logger, CancellationToken token)
    {
        var export = exporter.CreateTenantExport();

        await this.SendZip(
            export.FileName,
            async target => await exporter.WriteTenantExport(export, target, token),
            exception => logger.LogError(exception, "The tenant export was cut short"),
            token);

        return new EmptyResult();
    }

    // IFormFile.ContentType is never null, only empty, when a multipart part carries no Content-Type header.
    private static string? NormalizeMediaType(string mediaType)
    {
        return string.IsNullOrEmpty(mediaType) ? null : mediaType;
    }

    private FileUpload? BuildFileUpload(IFormFile file, out ActionResult? error)
    {
        if (file.Length > FileLimits.MaxUploadBytes)
        {
            error = this.Problem(detail: "The file exceeds the 100 MB upload limit.", statusCode: StatusCodes.Status413PayloadTooLarge, title: "File too large");
            return null;
        }

        var fileName = FileNameRules.LastSegment(file.FileName);
        var mediaType = NormalizeMediaType(file.ContentType);
        error = this.InvalidUploadMetadataProblem(fileName, mediaType);

        if (error is not null)
            return null;

        return new FileUpload
        {
            FileName = fileName,
            MediaType = mediaType,
            Content = file.OpenReadStream(),
        };
    }

    private async Task SendFileArchive(IFileArchiveReader archives, ILogger logger, FileArchive archive, CancellationToken token)
    {
        await this.SendZip(
            archive.FileName,
            async target => await archives.WriteFileArchive(archive, target, token),
            exception => logger.LogError(exception, "File archive of {OwnerId} was cut short", archive.OwnerId),
            token);
    }

    /// <summary>
    /// A failure before the first byte is an ordinary error response. A failure after it aborts the response, so the
    /// browser fails the download instead of saving a cut archive.
    /// </summary>
    private async Task SendZip(string fileName, Func<Stream, Task> write, Action<Exception> logCut, CancellationToken token)
    {
        var disposition = new ContentDispositionHeaderValue("attachment");
        disposition.SetHttpFileName(fileName);
        this.Response.Headers.ContentDisposition = disposition.ToString();
        this.Response.ContentType = "application/zip";

        try
        {
            await write(this.Response.Body);
        }
        catch (Exception exception) when (this.Response.HasStarted)
        {
            if (!token.IsCancellationRequested)
                logCut(exception);

            this.HttpContext.Abort();
        }
    }

    private ActionResult ExtensionMismatchProblem()
    {
        this.ModelState.AddModelError("fileName", "The new version must keep the extension of the file.");

        return this.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
    }

    private ActionResult? InvalidUploadMetadataProblem(string fileName, string? mediaType)
    {
        if (FileNameRules.Violation(fileName) is { } violation)
        {
            this.ModelState.AddModelError("fileName", violation);
            return this.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        }

        if (mediaType is { Length: > FileLimits.MaxMediaTypeLength })
        {
            this.ModelState.AddModelError("mediaType", $"The media type must not exceed {FileLimits.MaxMediaTypeLength} characters.");
            return this.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }
}
