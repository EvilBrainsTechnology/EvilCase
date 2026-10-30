using EvilBrains.EvilCase.Domain.ApiTokens;

namespace EvilBrains.EvilCase.Api.Contract.ApiTokens;

public sealed record ApiTokenItem
{
    public required Guid ApiTokenId { get; init; }

    public required string Name { get; init; }

    public required ApiTokenScope Scope { get; init; }

    public required DateTime Created { get; init; }

    public required DateTime Expires { get; init; }

    public DateTime? LastUsed { get; init; }
}
