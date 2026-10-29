using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files.Signatures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/files/{fileId:guid}/signature")]
public class FileSignaturesController : ControllerBase
{
    [HttpGet("")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileSignatureResponse>> ReadFileSignature([FromServices] IFileSignatureReader signatures, [FromRoute] Guid fileId, [FromQuery] int? version, CancellationToken token)
    {
        var signature = await signatures.ReadFileSignature(fileId, version, token);

        return signature is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound)
            : this.Ok(signature);
    }
}
