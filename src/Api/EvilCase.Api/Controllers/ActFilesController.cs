using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Files;
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
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.NotFound)
            : this.Ok(new FileListResponse { Items = items });
    }
}
