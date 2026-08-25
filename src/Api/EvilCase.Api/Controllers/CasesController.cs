using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
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

    [HttpPost("")]
    public Task<CaseListItem> CreateCase([FromServices] ICaseWriter writer, [FromBody] CreateCaseRequest request, CancellationToken token)
    {
        return writer.CreateCase(request, token);
    }

    [HttpGet("{caseId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaseDetail>> GetCase([FromServices] ICaseReader cases, [FromRoute] Guid caseId, CancellationToken token)
    {
        var @case = await cases.GetCaseDetail(caseId, token);

        return @case is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Case not found")
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

        switch (outcome)
        {
            case CaseUpdateOutcome.Updated:
                return this.NoContent();
            case CaseUpdateOutcome.NotFound:
                return this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Case not found");
            case CaseUpdateOutcome.CaseNumberTaken:
                return this.Problem(
                    detail: "Another case already carries the number.", statusCode: StatusCodes.Status409Conflict, title: "Case number taken");
            case CaseUpdateOutcome.InvalidCaseNumber:
                this.ModelState.AddModelError(nameof(CaseEditRequest.CaseNumber), "The case number must read EC/yyyyMMdd-nnn.");

                return this.ValidationProblem();
            default:
                throw new UnreachableException();
        }
    }
}
