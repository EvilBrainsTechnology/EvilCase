using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
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
        var items = await cases.ListCases(request, cancellationToken);

        return new CaseListResponse { Items = items };
    }

    [HttpPost("")]
    public Task<CaseListItem> CreateCase([FromServices] ICaseWriter writer, [FromBody] CreateCaseRequest request, CancellationToken cancellationToken)
    {
        return writer.CreateCase(request, cancellationToken);
    }
}
