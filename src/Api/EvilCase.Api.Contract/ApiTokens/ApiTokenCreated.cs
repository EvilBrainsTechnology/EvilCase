namespace EvilBrains.EvilCase.Api.Contract.ApiTokens;

public sealed record ApiTokenCreated
{
    public required ApiTokenItem Token { get; init; }

    /// <summary>
    /// The only response that carries the token; the server keeps its hash alone.
    /// </summary>
    public required string Secret { get; init; }
}
