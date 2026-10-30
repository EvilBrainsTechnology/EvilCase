using EvilBrains.EvilCase.Api.Contract.ApiTokens;

namespace EvilBrains.EvilCase.Auth;

public sealed record ApiTokenCreateResult
{
    public required ApiTokenCreateOutcome Outcome { get; init; }

    public ApiTokenCreated? Created { get; init; }
}
