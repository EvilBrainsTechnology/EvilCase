using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

internal interface IApiTokenStore
{
    /// <summary>
    /// False where the user already holds <paramref name="maxActive"/> tokens that have not expired at
    /// <paramref name="now"/>; nothing is written then.
    /// </summary>
    public Task<bool> AddApiToken(ApiToken apiToken, DateTime now, int maxActive, CancellationToken token);

    /// <summary>
    /// Newest first, expired ones included.
    /// </summary>
    public Task<IReadOnlyList<ApiToken>> ListApiTokens(Guid userId, CancellationToken token);

    /// <summary>
    /// With its user, whatever the tenant: a token is presented before a tenant is known.
    /// </summary>
    public Task<ApiToken?> FindApiToken(string tokenHash, CancellationToken token);

    /// <summary>
    /// False where the user holds no such token.
    /// </summary>
    public Task<bool> DeleteApiToken(Guid userId, Guid apiTokenId, CancellationToken token);

    /// <summary>
    /// Stamps <paramref name="now"/> as the last use unless a use after <paramref name="staleBefore"/> is already stamped.
    /// </summary>
    public Task TouchApiToken(Guid apiTokenId, DateTime now, DateTime staleBefore, CancellationToken token);
}
