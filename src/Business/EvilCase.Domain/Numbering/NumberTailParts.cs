namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// What the tail of a number carries: the day it belongs to and its place in that day.
/// </summary>
public sealed record NumberTailParts
{
    public required DateOnly Date { get; init; }

    public required int Sequence { get; init; }
}
