namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// What the tail of a number carries: the day it belongs to and its place in that day.
/// </summary>
public sealed record NumberTailParts(DateOnly Date, int Sequence);
