using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api")]
public class ActsController : ControllerBase
{
    [HttpGet("acts")]
    public async Task<ActListResponse> ListActs([FromServices] IActReader acts, [FromQuery] ActListRequest request, CancellationToken token)
    {
        return await acts.ListActs(request, token);
    }

    [HttpPost("acts")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActListItem>> CreateAct(
        [FromServices] IActWriter writer, [FromBody] CreateActRequest request, CancellationToken token)
    {
        var result = await writer.CreateAct(request, token);

        return result.Outcome switch
        {
            ActCreateOutcome.Created => this.CreatedAtAction(nameof(this.GetAct), new { actId = result.Act!.ActId }, result.Act),
            ActCreateOutcome.CaseNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: CaseProblems.NotFound),
            ActCreateOutcome.ContactNotFound => this.Problem(
                detail: "The contact named in the request does not exist.",
                statusCode: StatusCodes.Status409Conflict,
                title: ContactProblems.UnknownContact),
            ActCreateOutcome.LabelNotFound => this.Problem(
                detail: "A label named in the request does not exist.",
                statusCode: StatusCodes.Status409Conflict,
                title: LabelProblems.UnknownLabel),
            _ => throw new UnreachableException(),
        };
    }

    [HttpGet("acts/{actId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActDetail>> GetAct([FromServices] IActReader acts, [FromRoute] Guid actId, CancellationToken token)
    {
        var act = await acts.GetActDetail(actId, token);

        return act is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound)
            : this.Ok(act);
    }

    [HttpPut("acts/{actId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> EditAct(
        [FromServices] IActWriter writer, [FromRoute] Guid actId, [FromBody] ActEditRequest request, CancellationToken token)
    {
        var outcome = await writer.UpdateAct(actId, request, token);

        return outcome switch
        {
            ActUpdateOutcome.Updated => this.NoContent(),
            ActUpdateOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound),
            ActUpdateOutcome.ContactNotFound => this.Problem(
                detail: "The contact named in the request does not exist.",
                statusCode: StatusCodes.Status409Conflict,
                title: ContactProblems.UnknownContact),
            ActUpdateOutcome.ActNumberTaken => this.Problem(
                detail: "Another act already carries the number.", statusCode: StatusCodes.Status409Conflict, title: "Act number taken"),
            ActUpdateOutcome.InvalidActNumber => this.InvalidActNumberProblem(),
            ActUpdateOutcome.LabelNotFound => this.Problem(
                detail: "A label named in the request does not exist.",
                statusCode: StatusCodes.Status409Conflict,
                title: LabelProblems.UnknownLabel),
            _ => throw new UnreachableException(),
        };
    }

    [HttpDelete("acts/{actId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAct([FromServices] IActWriter writer, [FromRoute] Guid actId, CancellationToken token)
    {
        var outcome = await writer.DeleteAct(actId, token);

        return outcome switch
        {
            DeleteOutcome.Deleted => this.NoContent(),
            DeleteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound),
            _ => throw new UnreachableException(),
        };
    }

    private ActionResult InvalidActNumberProblem()
    {
        this.ModelState.AddModelError(nameof(ActEditRequest.ActNumber), "The act number must read <case-number>/yyyyMMdd-nnn.");

        return this.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
    }
}
