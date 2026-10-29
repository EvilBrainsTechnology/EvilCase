using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/acts/{actId:guid}/files")]
public class ActFilesController : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileListResponse>> ListActFiles([FromServices] IFileReader files, [FromRoute] Guid actId, CancellationToken token)
    {
        var items = await files.ListActFiles(actId, token);

        return items is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound)
            : this.Ok(new FileListResponse { Items = items });
    }

    [HttpPut("{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RenameActFile(
        [FromServices] IFileWriter writer, [FromRoute] Guid actId, [FromRoute] Guid fileId, [FromBody] FileRenameRequest request, CancellationToken token)
    {
        var fileName = FileNameRules.LastSegment(request.FileName);

        if (FileNameRules.Violation(fileName) is { } violation)
        {
            this.ModelState.AddModelError("fileName", violation);
            return this.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        }

        var outcome = await writer.RenameActFile(actId, fileId, fileName, token);

        return outcome switch
        {
            FileRenameOutcome.Renamed => this.NoContent(),
            FileRenameOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound),
            FileRenameOutcome.NameTaken => this.Problem(detail: "Another file of the act already carries the name.", statusCode: StatusCodes.Status409Conflict, title: FileProblems.NameTaken),
            _ => throw new UnreachableException(),
        };
    }

    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteActFile([FromServices] IFileWriter writer, [FromRoute] Guid actId, [FromRoute] Guid fileId, CancellationToken token)
    {
        var outcome = await writer.DeleteActFile(actId, fileId, token);

        return outcome switch
        {
            DeleteOutcome.Deleted => this.NoContent(),
            DeleteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }
}
