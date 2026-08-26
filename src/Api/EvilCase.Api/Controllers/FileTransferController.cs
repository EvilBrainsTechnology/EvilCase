using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

/// <summary>
/// The two file endpoints the client generator cannot express: a multipart upload and a byte stream. The
/// frontend reaches both through its own transfer client.
/// </summary>
[ApiController]
[Route("api")]
public class FileTransferController : ControllerBase
{
    [HttpPost("cases/{caseId:guid}/files")]
    [RequestSizeLimit(FileLimits.MaxUploadRequestBytes)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<FileListItem>> UploadCaseFile([FromServices] IFileWriter writer, [FromRoute] Guid caseId, [FromForm] IFormFile file, CancellationToken token)
    {
        if (file.Length > FileLimits.MaxUploadBytes)
            return this.Problem(detail: "The file exceeds the 100 MB upload limit.", statusCode: StatusCodes.Status413PayloadTooLarge, title: "File too large");

        await using var content = file.OpenReadStream();

        var upload = new FileUpload
        {
            // The browser may send a path; only the name is stored.
            FileName = Path.GetFileName(file.FileName),
            MediaType = file.ContentType,
            Content = content,
        };

        var created = await writer.UploadCaseFile(caseId, upload, token);

        return created is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Case not found")
            : this.CreatedAtAction(nameof(this.DownloadFileContent), new { fileId = created.Id }, created);
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
}
