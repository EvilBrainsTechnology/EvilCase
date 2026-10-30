using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Business.Files.Links;
using EvilBrains.EvilCase.Domain.Files;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

/// <summary>
/// Anonymous: the one-time link is the credential, and its use runs as the user and tenant that created it. No
/// [GenerateApiClient]: the MCP tools hand the link out, and the generator cannot express a byte stream.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route(FileLinkRoute.Template)]
public class FileLinksController : ControllerBase
{
    private const string FileTransferController = "FileTransfer";

    [HttpGet("")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadLinkedFile(
        [FromServices] IFileLinkWriter links,
        [FromServices] IFileReader files,
        [FromServices] IUserContext userContext,
        [FromQuery(Name = FileLinkRoute.SecretParameter)] string secret,
        CancellationToken token)
    {
        var link = await links.ConsumeFileLink(secret, token);

        if (link is not { Kind: FileLinkKind.Download })
            return this.LinkNotFoundProblem();

        using var entered = userContext.Enter(link.TenantId, link.UserId);

        var download = await files.OpenFileContent(link.TargetId, link.Version, token);

        return download is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound)
            : this.FileContent(download);
    }

    [HttpPut("")]
    [RequestSizeLimit(FileLimits.MaxUploadBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<FileListItem>> UploadLinkedFile(
        [FromServices] IFileLinkWriter links,
        [FromServices] IFileWriter writer,
        [FromServices] IUserContext userContext,
        [FromQuery(Name = FileLinkRoute.SecretParameter)] string secret,
        [FromQuery(Name = FileLinkRoute.FileNameParameter)] string fileName,
        CancellationToken token)
    {
        if (this.Request.ContentLength > FileLimits.MaxUploadBytes)
            return this.FileTooLargeProblem();

        var lastSegment = FileNameRules.LastSegment(fileName);
        var mediaType = string.IsNullOrEmpty(this.Request.ContentType) ? null : this.Request.ContentType;

        if (this.InvalidUploadMetadataProblem(lastSegment, mediaType) is { } problem)
            return problem;

        var link = await links.ConsumeFileLink(secret, token);

        if (link is null or { Kind: FileLinkKind.Download })
            return this.LinkNotFoundProblem();

        using var entered = userContext.Enter(link.TenantId, link.UserId);

        var upload = new FileUpload { FileName = lastSegment, MediaType = mediaType, Content = this.Request.Body };

        return link.Kind switch
        {
            FileLinkKind.CaseUpload => this.Answer(await writer.UploadCaseFile(link.TargetId, upload, token), CaseProblems.NotFound),
            FileLinkKind.ActUpload => this.Answer(await writer.UploadActFile(link.TargetId, upload, token), ActProblems.ActNotFound),
            FileLinkKind.VersionUpload => this.Answer(await writer.UploadFileVersion(link.TargetId, upload, token)),
            FileLinkKind.Download => throw new UnreachableException(),
            _ => throw new UnreachableException(),
        };
    }

    private ActionResult<FileListItem> Answer(UploadFileResult result, string ownerNotFound)
    {
        return result.Outcome switch
        {
            UploadFileOutcome.Uploaded or UploadFileOutcome.VersionAdded => this.CreatedAtAction(
                nameof(Controllers.FileTransferController.DownloadFileContent), FileTransferController, new { fileId = result.File!.FileId }, result.File),
            UploadFileOutcome.Unchanged => this.Ok(result.File),
            UploadFileOutcome.OwnerNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: ownerNotFound),
            _ => throw new UnreachableException(),
        };
    }

    private ActionResult<FileListItem> Answer(FileVersionUploadResult result)
    {
        return result.Outcome switch
        {
            FileVersionUploadOutcome.VersionAdded => this.CreatedAtAction(
                nameof(Controllers.FileTransferController.DownloadFileContent),
                FileTransferController,
                new { fileId = result.File!.FileId, version = result.File.Version },
                result.File),
            FileVersionUploadOutcome.Unchanged => this.Ok(result.File),
            FileVersionUploadOutcome.FileNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound),
            FileVersionUploadOutcome.ExtensionMismatch => this.ExtensionMismatchProblem(),
            _ => throw new UnreachableException(),
        };
    }

    private ActionResult LinkNotFoundProblem()
    {
        return this.Problem(detail: "The link is unknown, used or expired.", statusCode: StatusCodes.Status404NotFound, title: FileProblems.LinkNotFound);
    }
}
