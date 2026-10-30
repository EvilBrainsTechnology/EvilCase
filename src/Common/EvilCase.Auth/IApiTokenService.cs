using EvilBrains.EvilCase.Api.Contract.ApiTokens;

namespace EvilBrains.EvilCase.Auth;

public interface IApiTokenService
{
    public Task<IReadOnlyList<ApiTokenItem>> ListApiTokens(Guid userId, CancellationToken token);

    public Task<ApiTokenCreateResult> CreateApiToken(Guid userId, ApiTokenCreateRequest request, CancellationToken token);

    public Task<ApiTokenRevokeOutcome> RevokeApiToken(Guid userId, Guid apiTokenId, CancellationToken token);
}
