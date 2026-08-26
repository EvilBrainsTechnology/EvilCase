using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Acts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/cases/{caseId:guid}/acts")]
public class ActsController : ControllerBase
{
    [HttpGet("")]
    public async Task<ActListResponse> ListActs([FromServices] IActReader acts, [FromRoute] Guid caseId, CancellationToken token)
    {
        var items = await acts.ListActs(caseId, token);

        return new ActListResponse { Items = items };
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActListItem>> CreateAct(
        [FromServices] IActWriter writer, [FromRoute] Guid caseId, [FromBody] CreateActRequest request, CancellationToken token)
    {
        var result = await writer.CreateAct(caseId, request, token);

        return result.Outcome switch
        {
            // No Location header: the act detail endpoint does not exist yet.
            ActCreateOutcome.Created => this.Created((string?)null, result.Act),
            ActCreateOutcome.CaseNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Case not found"),
            ActCreateOutcome.ContactNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Contact not found"),
            _ => throw new UnreachableException(),
        };
    }
}
