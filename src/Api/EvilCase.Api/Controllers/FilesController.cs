using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/files/{fileId:guid}")]
public class FilesController : ControllerBase
{
    [HttpPut("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RenameFile([FromServices] IFileWriter writer, [FromRoute] Guid fileId, [FromBody] FileRenameRequest request, CancellationToken token)
    {
        var fileName = FileNameRules.LastSegment(request.FileName);

        if (this.FileNameProblem(fileName) is { } problem)
            return problem;

        return this.RenameAnswer(await writer.RenameFile(fileId, fileName, token));
    }

    [HttpDelete("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteFile([FromServices] IFileWriter writer, [FromRoute] Guid fileId, CancellationToken token)
    {
        var outcome = await writer.DeleteFile(fileId, token);

        return outcome switch
        {
            DeleteOutcome.Deleted => this.NoContent(),
            DeleteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }
}
