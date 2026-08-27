using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Acts;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

/// <summary>
/// The tenant's acts across every case (SDD-005); the nested list under a case is ActsController.
/// </summary>
[ApiController]
[GenerateApiClient]
[Route("api/acts")]
public class TenantActsController : ControllerBase
{
    [HttpGet("")]
    public async Task<ActListResponse> ListTenantActs([FromServices] IActReader acts, [FromQuery] ActListRequest request, CancellationToken token)
    {
        var items = await acts.ListTenantActs(request, token);

        return new ActListResponse { Items = items };
    }
}
