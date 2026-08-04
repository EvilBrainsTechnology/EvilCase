namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// One step of the path from the root case down to the one being read.
/// </summary>
public sealed record CaseAncestor
{
    public required long Id { get; init; }

    public required string CaseNumber { get; init; }

    public required string Title { get; init; }
}
