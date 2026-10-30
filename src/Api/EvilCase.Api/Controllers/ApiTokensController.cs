using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.ApiTokens;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

/// <summary>
/// A signed-in session manages its own tokens; a token never reaches here (SDD-006).
/// </summary>
[ApiController]
[GenerateApiClient]
[Route(ApiTokenRoute.Template)]
public class ApiTokensController : ControllerBase
{
    [HttpGet("")]
    public async Task<ApiTokenListResponse> ListApiTokens(
        [FromServices] IApiTokenService apiTokens, [FromServices] IUserContext userContext, CancellationToken token)
    {
        var items = await apiTokens.ListApiTokens(userContext.UserId, token);

        return new ApiTokenListResponse { Items = items };
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiTokenCreated>> CreateApiToken(
        [FromServices] IApiTokenService apiTokens,
        [FromServices] IUserContext userContext,
        [FromBody] ApiTokenCreateRequest request,
        CancellationToken token)
    {
        var result = await apiTokens.CreateApiToken(userContext.UserId, request, token);

        return result.Outcome switch
        {
            ApiTokenCreateOutcome.Created => this.StatusCode(StatusCodes.Status201Created, result.Created),
            ApiTokenCreateOutcome.LimitReached => this.Problem(
                detail: $"At most {ApiTokenLimits.MaxActive} unexpired tokens per user.",
                statusCode: StatusCodes.Status400BadRequest,
                title: ApiTokenProblems.LimitReached),
            _ => throw new UnreachableException(),
        };
    }

    [HttpDelete("{apiTokenId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RevokeApiToken(
        [FromServices] IApiTokenService apiTokens, [FromServices] IUserContext userContext, [FromRoute] Guid apiTokenId, CancellationToken token)
    {
        var outcome = await apiTokens.RevokeApiToken(userContext.UserId, apiTokenId, token);

        return outcome switch
        {
            ApiTokenRevokeOutcome.Revoked => this.NoContent(),
            ApiTokenRevokeOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: ApiTokenProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }
}
