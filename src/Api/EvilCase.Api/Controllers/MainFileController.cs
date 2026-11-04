using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/files/{fileId:guid}/main")]
public class MainFileController : ControllerBase
{
    [HttpPut("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetMainFile([FromServices] IFileWriter writer, [FromRoute] Guid fileId, CancellationToken token)
    {
        return this.Answer(await writer.SetMainFile(fileId, token));
    }

    [HttpDelete("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UnsetMainFile([FromServices] IFileWriter writer, [FromRoute] Guid fileId, CancellationToken token)
    {
        return this.Answer(await writer.UnsetMainFile(fileId, token));
    }

    private ActionResult Answer(MainFileOutcome outcome)
    {
        return outcome switch
        {
            MainFileOutcome.Changed => this.NoContent(),
            MainFileOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }
}
