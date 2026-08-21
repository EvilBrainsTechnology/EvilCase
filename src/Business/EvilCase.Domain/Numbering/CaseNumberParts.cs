namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// What a case number says: the day it was issued for and the sequence inside that day.
/// </summary>
public sealed record CaseNumberParts(DateOnly Date, int Sequence);
