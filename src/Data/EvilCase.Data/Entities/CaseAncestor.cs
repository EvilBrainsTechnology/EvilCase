namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// A row of the case_ancestors function: a case above the case asked for, depth 1 being its parent.
/// </summary>
public sealed record CaseAncestor
{
    public Guid CaseId { get; init; }

    public int Depth { get; init; }
}
