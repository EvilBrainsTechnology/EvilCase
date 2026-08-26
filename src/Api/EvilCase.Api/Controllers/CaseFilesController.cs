using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
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
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Case not found")
            : this.Ok(new FileListResponse { Items = items });
    }

    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteCaseFile([FromServices] IFileWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid fileId, CancellationToken token)
    {
        var outcome = await writer.DeleteCaseFile(caseId, fileId, token);

        return outcome switch
        {
            FileDeleteOutcome.Deleted => this.NoContent(),
            FileDeleteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: "File not found"),
            _ => throw new UnreachableException(),
        };
    }
}
