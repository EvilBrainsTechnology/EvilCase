using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/cases")]
public class CasesController(ICaseReader cases, ICaseCommentWriter comments) : ControllerBase
{
    [HttpGet("list")]
    public async Task<CaseListResponse> ListCases([FromQuery] CaseListRequest request, CancellationToken cancellationToken)
    {
        var items = await cases.List(request, cancellationToken);

        return new CaseListResponse { Items = items };
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CaseDetailResponse>> GetCase([FromRoute] long id, CancellationToken cancellationToken)
    {
        var detail = await cases.Detail(id, cancellationToken);

        if (detail is null)
            return this.NotFound();

        return this.Ok(detail);
    }

    [HttpPost("{id:long}/comments")]
    public async Task<ActionResult<CaseComment>> AddCaseComment(
        [FromRoute] long id,
        [FromBody] AddCaseCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await comments.Add(id, request, cancellationToken);

        if (comment is null)
            return this.NotFound();

        return this.Ok(comment);
    }
}
