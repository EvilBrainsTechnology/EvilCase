using EvilBrains.EvilCase.Api.Contract.ApiTokens;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Auth;

internal sealed class ApiTokenService(IApiTokenStore apiTokenStore, TimeProvider timeProvider, ILogger<ApiTokenService> logger) : IApiTokenService
{
    public async Task<IReadOnlyList<ApiTokenItem>> ListApiTokens(Guid userId, CancellationToken token)
    {
        var apiTokens = await apiTokenStore.ListApiTokens(userId, token);

        return [.. apiTokens.Select(Describe)];
    }

    public async Task<ApiTokenCreateResult> CreateApiToken(Guid userId, ApiTokenCreateRequest request, CancellationToken token)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var secret = ApiTokenValue.Create();

        var apiToken = new ApiToken
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Scope = request.Scope,
            TokenHash = ApiTokenValue.Hash(secret),
            Expires = now.AddDays(request.ExpiresInDays),
        };

        if (!await apiTokenStore.AddApiToken(apiToken, now, ApiTokenLimits.MaxActive, token))
            return new() { Outcome = ApiTokenCreateOutcome.LimitReached };

        logger.LogInformation("API token {ApiTokenId} was created for user {UserId} with scope {ApiTokenScope}", apiToken.Id, userId, apiToken.Scope);

        return new()
        {
            Outcome = ApiTokenCreateOutcome.Created,
            Created = new() { Token = Describe(apiToken), Secret = secret },
        };
    }

    public async Task<ApiTokenRevokeOutcome> RevokeApiToken(Guid userId, Guid apiTokenId, CancellationToken token)
    {
        if (!await apiTokenStore.DeleteApiToken(userId, apiTokenId, token))
            return ApiTokenRevokeOutcome.NotFound;

        logger.LogInformation("API token {ApiTokenId} of user {UserId} was revoked", apiTokenId, userId);

        return ApiTokenRevokeOutcome.Revoked;
    }

    private static ApiTokenItem Describe(ApiToken apiToken)
    {
        return new()
        {
            ApiTokenId = apiToken.Id,
            Name = apiToken.Name,
            Scope = apiToken.Scope,
            Created = apiToken.Created,
            Expires = apiToken.Expires,
            LastUsed = apiToken.LastUsed,
        };
    }
}
