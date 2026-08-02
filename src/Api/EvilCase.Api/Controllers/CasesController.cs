using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Cases;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/cases")]
public class CasesController(ICaseReader cases) : ControllerBase
{
    [HttpGet("list")]
    public async Task<CaseListResponse> ListCases([FromQuery] CaseListRequest request, CancellationToken cancellationToken)
    {
        var items = await cases.List(request, cancellationToken);

        return new CaseListResponse { Items = items };
    }
}
