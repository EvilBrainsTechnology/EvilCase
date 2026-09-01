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
[Route("api/cases/{caseId:guid}/acts/{actId:guid}/files")]
public class ActFilesController : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileListResponse>> ListActFiles([FromServices] IFileReader files, [FromRoute] Guid caseId, [FromRoute] Guid actId, CancellationToken token)
    {
        var items = await files.ListActFiles(caseId, actId, token);

        return items is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound)
            : this.Ok(new FileListResponse { Items = items });
    }

    [HttpDelete("{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteActFile([FromServices] IFileWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid actId, [FromRoute] Guid fileId, CancellationToken token)
    {
        var outcome = await writer.DeleteActFile(caseId, actId, fileId, token);

        return outcome switch
        {
            DeleteOutcome.Deleted => this.NoContent(),
            DeleteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }
}
