using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/cases/{caseId:guid}/files")]
public class CaseFilesController : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileListResponse>> ListCaseFiles([FromServices] IFileReader files, [FromRoute] Guid caseId, CancellationToken token)
    {
        var items = await files.ListCaseFiles(caseId, token);

        return items is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: CaseProblems.NotFound)
            : this.Ok(new FileListResponse { Items = items });
    }

    [HttpPut("{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RenameCaseFile(
        [FromServices] IFileWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid fileId, [FromBody] FileRenameRequest request, CancellationToken token)
    {
        var fileName = FileNameRules.LastSegment(request.FileName);

        if (FileNameRules.Violation(fileName) is { } violation)
        {
            this.ModelState.AddModelError("fileName", violation);
            return this.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        }

        var outcome = await writer.RenameCaseFile(caseId, fileId, fileName, token);

        return outcome switch
        {
            FileRenameOutcome.Renamed => this.NoContent(),
            FileRenameOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }

    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteCaseFile([FromServices] IFileWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid fileId, CancellationToken token)
    {
        var outcome = await writer.DeleteCaseFile(caseId, fileId, token);

        return outcome switch
        {
            DeleteOutcome.Deleted => this.NoContent(),
            DeleteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }
}
