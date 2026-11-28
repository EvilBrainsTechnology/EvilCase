using EvilBrains.EvilCase.Api.Contract.Acts;
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

        return this.Answer(await writer.UploadActFile(actId, upload, token));
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

        return this.Answer(await writer.UploadFileVersion(fileId, upload, token));
    }

    [HttpGet("files/{fileId:guid}/content")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadFileContent([FromServices] IFileReader files, [FromRoute] Guid fileId, [FromQuery] int? version, CancellationToken token)
    {
        var download = await files.OpenFileContent(fileId, version, token);

        return download is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound)
            : this.FileContent(download);
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
            return this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.NotFound);

        await this.SendZip(
            archive.FileName,
            async target => await archives.WriteFileArchive(archive, target, token),
            exception => logger.LogError(exception, "File archive of {ActId} was cut short", archive.ActId),
            token);

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

    private FileUpload? BuildFileUpload(IFormFile file, out ActionResult? error)
    {
        if (file.Length > FileLimits.MaxUploadBytes)
        {
            error = this.FileTooLargeProblem();
            return null;
        }

        var fileName = FileNameRules.LastSegment(file.FileName);

        // IFormFile.ContentType is never null, only empty, when a multipart part carries no Content-Type header.
        var mediaType = string.IsNullOrEmpty(file.ContentType) ? null : file.ContentType;
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
}
