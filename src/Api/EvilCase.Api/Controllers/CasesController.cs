using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/cases")]
public class CasesController : ControllerBase
{
    [HttpGet("")]
    public async Task<CaseListResponse> ListCases([FromServices] ICaseReader cases, [FromQuery] CaseListRequest request, CancellationToken token)
    {
        var items = await cases.ListCases(request, token);

        return new CaseListResponse { Items = items };
    }

    [HttpGet("counts")]
    public async Task<CaseStatusCounts> CountCases([FromServices] ICaseReader cases, CancellationToken token)
    {
        return await cases.CountCasesByStatus(token);
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CaseListItem>> CreateCase([FromServices] ICaseWriter writer, [FromBody] CreateCaseRequest request, CancellationToken token)
    {
        var result = await writer.CreateCase(request, token);

        return result.Outcome switch
        {
            CaseCreateOutcome.Created => this.CreatedAtAction(nameof(this.GetCase), new { caseId = result.Case!.CaseId }, result.Case),
            CaseCreateOutcome.InvalidParent => this.Problem(
                detail: "The parent case does not exist.", statusCode: StatusCodes.Status409Conflict, title: CaseProblems.InvalidParent),
            _ => throw new UnreachableException(),
        };
    }

    [HttpGet("{caseId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaseDetail>> GetCase([FromServices] ICaseReader cases, [FromRoute] Guid caseId, CancellationToken token)
    {
        var @case = await cases.GetCaseDetail(caseId, token);

        return @case is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: CaseProblems.NotFound)
            : this.Ok(@case);
    }

    [HttpPut("{caseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> EditCase([FromServices] ICaseWriter writer, [FromRoute] Guid caseId, [FromBody] CaseEditRequest request, CancellationToken token)
    {
        var outcome = await writer.UpdateCase(caseId, request, token);

        return outcome switch
        {
            CaseUpdateOutcome.Updated => this.NoContent(),
            CaseUpdateOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: CaseProblems.NotFound),
            CaseUpdateOutcome.CaseNumberTaken => this.Problem(
                detail: "Another case already carries the number.", statusCode: StatusCodes.Status409Conflict, title: "Case number taken"),
            CaseUpdateOutcome.InvalidCaseNumber => this.InvalidCaseNumberProblem(),
            CaseUpdateOutcome.InvalidParent => this.Problem(
                detail: "The parent case must exist and must be neither the case itself nor one of its subordinates.",
                statusCode: StatusCodes.Status409Conflict,
                title: CaseProblems.InvalidParent),
            _ => throw new UnreachableException(),
        };
    }

    [HttpDelete("{caseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteCase([FromServices] ICaseWriter writer, [FromRoute] Guid caseId, CancellationToken token)
    {
        var outcome = await writer.DeleteCase(caseId, token);

        return outcome switch
        {
            DeleteOutcome.Deleted => this.NoContent(),
            DeleteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: CaseProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPost("{caseId:guid}/external-numbers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AddExternalCaseNumber(
        [FromServices] IExternalCaseNumberWriter writer,
        [FromRoute] Guid caseId,
        [FromBody] ExternalNumberRequest request,
        CancellationToken token)
    {
        var outcome = await writer.AddExternalCaseNumber(caseId, request, token);

        return outcome switch
        {
            ExternalCaseNumberOutcome.Added => this.NoContent(),
            ExternalCaseNumberOutcome.CaseNotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: CaseProblems.NotFound),
            ExternalCaseNumberOutcome.UnknownContact => this.Problem(
                detail: "The contact that assigned the mark does not exist.",
                statusCode: StatusCodes.Status409Conflict,
                title: ContactProblems.UnknownContact),
            ExternalCaseNumberOutcome.ValueTaken => this.Problem(
                detail: "The case already carries the mark.",
                statusCode: StatusCodes.Status409Conflict,
                title: ExternalNumberProblems.Taken),
            _ => throw new UnreachableException(),
        };
    }

    [HttpDelete("{caseId:guid}/external-numbers/{numberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteExternalCaseNumber(
        [FromServices] IExternalCaseNumberWriter writer,
        [FromRoute] Guid caseId,
        [FromRoute] Guid numberId,
        CancellationToken token)
    {
        var outcome = await writer.DeleteExternalCaseNumber(caseId, numberId, token);

        return outcome switch
        {
            DeleteOutcome.Deleted => this.NoContent(),
            DeleteOutcome.NotFound => this.Problem(
                statusCode: StatusCodes.Status404NotFound, title: "External case number not found"),
            _ => throw new UnreachableException(),
        };
    }

    private ActionResult InvalidCaseNumberProblem()
    {
        this.ModelState.AddModelError(nameof(CaseEditRequest.CaseNumber), "The case number must read EC/yyyyMMdd-nnn.");

        return this.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
    }
}
