using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

/// <summary>
/// A new version arrives as multipart through FileTransferController.
/// </summary>
[ApiController]
[GenerateApiClient]
[Route("api/files/{fileId:guid}/versions")]
public class FileVersionsController : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileVersionListResponse>> ListFileVersions([FromServices] IFileReader files, [FromRoute] Guid fileId, CancellationToken token)
    {
        var items = await files.ListFileVersions(fileId, token);

        return items is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound)
            : this.Ok(new FileVersionListResponse { Items = items });
    }

    [HttpDelete("{number:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteFileVersion([FromServices] IFileWriter writer, [FromRoute] Guid fileId, [FromRoute] int number, CancellationToken token)
    {
        var outcome = await writer.DeleteFileVersion(fileId, number, token);

        return outcome switch
        {
            FileVersionDeleteOutcome.Deleted => this.NoContent(),
            FileVersionDeleteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.VersionNotFound),
            FileVersionDeleteOutcome.LastVersion => this.Problem(
                detail: "The only version of a file is deleted only with the file.", statusCode: StatusCodes.Status409Conflict, title: FileProblems.LastVersion),
            _ => throw new UnreachableException(),
        };
    }
}
