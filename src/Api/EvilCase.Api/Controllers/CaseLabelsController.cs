using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Labels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/cases/{caseId:guid}/labels")]
public class CaseLabelsController : ControllerBase
{
    [HttpPut("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> SetCaseLabels(
        [FromServices] ILabelAssignmentWriter writer, [FromRoute] Guid caseId, [FromBody] LabelAssignmentRequest request, CancellationToken token)
    {
        var outcome = await writer.SetCaseLabels(caseId, request, token);

        return outcome switch
        {
            LabelAssignmentOutcome.Assigned => this.NoContent(),
            LabelAssignmentOutcome.OwnerNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: CaseProblems.NotFound),
            LabelAssignmentOutcome.LabelNotFound => this.Problem(
                detail: "A label named in the request does not exist.",
                statusCode: StatusCodes.Status409Conflict,
                title: LabelProblems.UnknownLabel),
            _ => throw new UnreachableException(),
        };
    }
}
