using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Labels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/labels")]
public class LabelsController : ControllerBase
{
    [HttpGet("")]
    public async Task<LabelListResponse> ListLabels([FromServices] ILabelReader labels, CancellationToken token)
    {
        var items = await labels.ListLabels(token);

        return new LabelListResponse { Items = items };
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LabelItem>> CreateLabel([FromServices] ILabelWriter writer, [FromBody] LabelEditRequest request, CancellationToken token)
    {
        var result = await writer.CreateLabel(request, token);

        return result.Outcome switch
        {
            LabelCreateOutcome.Created => this.CreatedAtAction(nameof(this.ListLabels), routeValues: null, result.Label),
            LabelCreateOutcome.NameTaken => this.NameTakenProblem(),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPut("{labelId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> EditLabel(
        [FromServices] ILabelWriter writer, [FromRoute] Guid labelId, [FromBody] LabelEditRequest request, CancellationToken token)
    {
        var outcome = await writer.UpdateLabel(labelId, request, token);

        return outcome switch
        {
            LabelUpdateOutcome.Updated => this.NoContent(),
            LabelUpdateOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: LabelProblems.NotFound),
            LabelUpdateOutcome.NameTaken => this.NameTakenProblem(),
            _ => throw new UnreachableException(),
        };
    }

    [HttpDelete("{labelId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteLabel([FromServices] ILabelWriter writer, [FromRoute] Guid labelId, CancellationToken token)
    {
        var outcome = await writer.DeleteLabel(labelId, token);

        return outcome switch
        {
            DeleteOutcome.Deleted => this.NoContent(),
            DeleteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: LabelProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }

    private ActionResult NameTakenProblem()
    {
        return this.Problem(detail: "Another label already carries the name.", statusCode: StatusCodes.Status409Conflict, title: LabelProblems.NameTaken);
    }
}
