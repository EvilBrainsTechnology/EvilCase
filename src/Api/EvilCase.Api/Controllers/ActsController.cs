using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Business.Acts;
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
        var items = await acts.ListActs(request, token);

        return new ActListResponse { Items = items };
    }

    [HttpGet("cases/{caseId:guid}/acts")]
    public async Task<ActListResponse> ListCaseActs([FromServices] IActReader acts, [FromRoute] Guid caseId, CancellationToken token)
    {
        var items = await acts.ListCaseActs(caseId, token);

        return new ActListResponse { Items = items };
    }

    [HttpPost("cases/{caseId:guid}/acts")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActListItem>> CreateAct(
        [FromServices] IActWriter writer, [FromRoute] Guid caseId, [FromBody] CreateActRequest request, CancellationToken token)
    {
        var result = await writer.CreateAct(caseId, request, token);

        return result.Outcome switch
        {
            ActCreateOutcome.Created => this.CreatedAtAction(nameof(this.GetAct), new { caseId, actId = result.Act!.ActId }, result.Act),
            ActCreateOutcome.CaseNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: CaseProblems.NotFound),
            ActCreateOutcome.ContactNotFound => this.Problem(
                detail: "The contact named in the request does not exist.",
                statusCode: StatusCodes.Status409Conflict,
                title: ContactProblems.UnknownContact),
            _ => throw new UnreachableException(),
        };
    }

    [HttpGet("cases/{caseId:guid}/acts/{actId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActDetail>> GetAct([FromServices] IActReader acts, [FromRoute] Guid caseId, [FromRoute] Guid actId, CancellationToken token)
    {
        var act = await acts.GetActDetail(caseId, actId, token);

        return act is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound)
            : this.Ok(act);
    }

    [HttpPut("cases/{caseId:guid}/acts/{actId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> EditAct(
        [FromServices] IActWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid actId, [FromBody] ActEditRequest request, CancellationToken token)
    {
        var outcome = await writer.UpdateAct(caseId, actId, request, token);

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
            _ => throw new UnreachableException(),
        };
    }

    [HttpDelete("cases/{caseId:guid}/acts/{actId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAct([FromServices] IActWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid actId, CancellationToken token)
    {
        var outcome = await writer.DeleteAct(caseId, actId, token);

        return outcome switch
        {
            ActDeleteOutcome.Deleted => this.NoContent(),
            ActDeleteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPost("cases/{caseId:guid}/acts/{actId:guid}/external-numbers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AddExternalActNumber(
        [FromServices] IExternalActNumberWriter writer,
        [FromRoute] Guid caseId,
        [FromRoute] Guid actId,
        [FromBody] ExternalNumberRequest request,
        CancellationToken token)
    {
        var outcome = await writer.AddExternalActNumber(caseId, actId, request, token);

        return outcome switch
        {
            ExternalActNumberOutcome.Added => this.NoContent(),
            ExternalActNumberOutcome.ActNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound),
            ExternalActNumberOutcome.UnknownContact => this.Problem(
                detail: "The contact that assigned the number does not exist.",
                statusCode: StatusCodes.Status409Conflict,
                title: ContactProblems.UnknownContact),
            ExternalActNumberOutcome.ValueTaken => this.Problem(
                detail: "The act already carries the number.",
                statusCode: StatusCodes.Status409Conflict,
                title: ExternalNumberProblems.Taken),
            _ => throw new UnreachableException(),
        };
    }

    [HttpDelete("cases/{caseId:guid}/acts/{actId:guid}/external-numbers/{numberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteExternalActNumber(
        [FromServices] IExternalActNumberWriter writer,
        [FromRoute] Guid caseId,
        [FromRoute] Guid actId,
        [FromRoute] Guid numberId,
        CancellationToken token)
    {
        var outcome = await writer.DeleteExternalActNumber(caseId, actId, numberId, token);

        return outcome switch
        {
            ExternalActNumberDeleteOutcome.Deleted => this.NoContent(),
            ExternalActNumberDeleteOutcome.NotFound => this.Problem(
                statusCode: StatusCodes.Status404NotFound, title: "External act number not found"),
            _ => throw new UnreachableException(),
        };
    }

    private ActionResult InvalidActNumberProblem()
    {
        this.ModelState.AddModelError(nameof(ActEditRequest.ActNumber), "The act number must read <case-number>/yyyyMMdd-nnn.");

        return this.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
    }
}
