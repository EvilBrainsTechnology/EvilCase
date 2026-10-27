using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files.Signatures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/files/{fileId:guid}/signers")]
public class FileSignersController : ControllerBase
{
    [HttpGet("")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileSignersResponse>> ListFileSigners([FromServices] IFileSignerReader signers, [FromRoute] Guid fileId, CancellationToken token)
    {
        var response = await signers.ListFileSigners(fileId, token);

        return response is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound)
            : this.Ok(response);
    }

    [HttpGet("verification")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileSignersResponse>> VerifyFileSigners([FromServices] IFileSignerVerifier verifier, [FromRoute] Guid fileId, CancellationToken token)
    {
        var response = await verifier.VerifyFileSigners(fileId, token);

        return response is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound)
            : this.Ok(response);
    }
}
