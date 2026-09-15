using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Labels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/cases/{caseId:guid}/acts/{actId:guid}/labels")]
public class ActLabelsController : ControllerBase
{
    [HttpPut("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> SetActLabels(
        [FromServices] ILabelAssignmentWriter writer,
        [FromRoute] Guid caseId,
        [FromRoute] Guid actId,
        [FromBody] LabelAssignmentRequest request,
        CancellationToken token)
    {
        var outcome = await writer.SetActLabels(caseId, actId, request, token);

        return outcome switch
        {
            LabelAssignmentOutcome.Assigned => this.NoContent(),
            LabelAssignmentOutcome.OwnerNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound),
            LabelAssignmentOutcome.LabelNotFound => this.Problem(
                detail: "A label named in the request does not exist.",
                statusCode: StatusCodes.Status409Conflict,
                title: LabelProblems.UnknownLabel),
            _ => throw new UnreachableException(),
        };
    }
}
