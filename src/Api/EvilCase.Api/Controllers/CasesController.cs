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
    public async Task<CaseListResponse> ListCases([FromServices] ICaseReader cases, [FromQuery] CaseListRequest request, CancellationToken cancellationToken)
    {
        var items = await cases.List(request, cancellationToken);

        return new CaseListResponse { Items = items };
    }

    [HttpPost("")]
    public Task<CaseListItem> CreateCase([FromServices] ICaseWriter writer, [FromBody] CreateCaseRequest request, CancellationToken cancellationToken)
    {
        return writer.Create(request, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaseDetail>> GetCase([FromServices] ICaseReader cases, [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var detail = await cases.Detail(id, cancellationToken);

        return detail is null ? this.NotFound() : this.Ok(detail);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateCase(
        [FromServices] ICaseWriter writer,
        [FromRoute] Guid id,
        [FromBody] UpdateCaseRequest request,
        CancellationToken cancellationToken)
    {
        var status = await writer.Update(id, request, cancellationToken);

        if (status == CaseUpdateStatus.InvalidNumber)
            this.ModelState.AddModelError(nameof(UpdateCaseRequest.CaseNumber), "The case number does not match the required format.");

        return status switch
        {
            CaseUpdateStatus.NotFound => this.NotFound(),
            CaseUpdateStatus.InvalidNumber => this.ValidationProblem(this.ModelState),
            CaseUpdateStatus.NumberTaken => this.Problem(statusCode: StatusCodes.Status409Conflict, title: "Case number already taken"),
            CaseUpdateStatus.Updated => this.NoContent(),
            _ => throw new UnreachableException("Unknown case update status."),
        };
    }
}
