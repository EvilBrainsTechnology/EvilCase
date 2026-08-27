using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

/// <summary>
/// The upload and download endpoints the client generator cannot express: a multipart upload for a case
/// and for an act, and a byte stream. The frontend reaches all three through its own transfer client.
/// </summary>
[ApiController]
[Route("api")]
public class FileTransferController : ControllerBase
{
    [HttpPost("cases/{caseId:guid}/files")]
    [RequestSizeLimit(FileLimits.MaxUploadRequestBytes)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<FileListItem>> UploadCaseFile([FromServices] IFileWriter writer, [FromRoute] Guid caseId, [FromForm] IFormFile file, CancellationToken token)
    {
        if (file.Length > FileLimits.MaxUploadBytes)
            return this.Problem(detail: "The file exceeds the 100 MB upload limit.", statusCode: StatusCodes.Status413PayloadTooLarge, title: "File too large");

        // The browser may send a path; only the name is stored.
        var fileName = Path.GetFileName(file.FileName);
        var invalidMetadata = this.InvalidUploadMetadataProblem(fileName, file.ContentType);

        if (invalidMetadata is not null)
            return invalidMetadata;

        await using var content = file.OpenReadStream();

        var upload = new FileUpload
        {
            FileName = fileName,
            MediaType = file.ContentType,
            Content = content,
        };

        var result = await writer.UploadCaseFile(caseId, upload, token);

        return result.Outcome switch
        {
            UploadFileOutcome.Uploaded => this.CreatedAtAction(nameof(this.DownloadFileContent), new { fileId = result.File!.FileId }, result.File),
            UploadFileOutcome.OwnerNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Case not found"),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPost("cases/{caseId:guid}/acts/{actId:guid}/files")]
    [RequestSizeLimit(FileLimits.MaxUploadRequestBytes)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<FileListItem>> UploadActFile(
        [FromServices] IFileWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid actId, [FromForm] IFormFile file, CancellationToken token)
    {
        if (file.Length > FileLimits.MaxUploadBytes)
            return this.Problem(detail: "The file exceeds the 100 MB upload limit.", statusCode: StatusCodes.Status413PayloadTooLarge, title: "File too large");

        // The browser may send a path; only the name is stored.
        var fileName = Path.GetFileName(file.FileName);
        var invalidMetadata = this.InvalidUploadMetadataProblem(fileName, file.ContentType);

        if (invalidMetadata is not null)
            return invalidMetadata;

        await using var content = file.OpenReadStream();

        var upload = new FileUpload
        {
            FileName = fileName,
            MediaType = file.ContentType,
            Content = content,
        };

        var result = await writer.UploadActFile(caseId, actId, upload, token);

        return result.Outcome switch
        {
            UploadFileOutcome.Uploaded => this.CreatedAtAction(nameof(this.DownloadFileContent), new { fileId = result.File!.FileId }, result.File),
            UploadFileOutcome.OwnerNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound),
            _ => throw new UnreachableException(),
        };
    }

    [HttpGet("files/{fileId:guid}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadFileContent([FromServices] IFileReader files, [FromRoute] Guid fileId, CancellationToken token)
    {
        var download = await files.OpenFileContent(fileId, token);

        if (download is null)
            return this.Problem(statusCode: StatusCodes.Status404NotFound, title: "File not found");

        // Always an attachment: a stored document is never rendered in place (SDD-012).
        // X-Content-Type-Options: nosniff already comes from SecurityHeadersMiddleware on every response.
        return this.File(download.Content, download.MediaType, download.FileName);
    }

    private ActionResult? InvalidUploadMetadataProblem(string fileName, string mediaType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            this.ModelState.AddModelError("fileName", "The file name must not be empty.");
            return this.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        }

        if (fileName.Length > FileLimits.MaxFileNameLength)
        {
            this.ModelState.AddModelError("fileName", $"The file name must not exceed {FileLimits.MaxFileNameLength} characters.");
            return this.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        }

        if (mediaType.Length > FileLimits.MaxMediaTypeLength)
        {
            this.ModelState.AddModelError("mediaType", $"The media type must not exceed {FileLimits.MaxMediaTypeLength} characters.");
            return this.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }
}
