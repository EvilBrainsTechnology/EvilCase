using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/cases")]
public class CasesController(ICaseReader cases, ICaseWriter writer) : ControllerBase
{
    [HttpGet("")]
    public async Task<CaseListResponse> ListCases([FromQuery] CaseListRequest request, CancellationToken cancellationToken)
    {
        var items = await cases.List(request, cancellationToken);

        return new CaseListResponse { Items = items };
    }

    [HttpPost("")]
    public Task<CaseListItem> CreateCase([FromBody] CreateCaseRequest request, CancellationToken cancellationToken)
    {
        return writer.Create(request, cancellationToken);
    }
}
