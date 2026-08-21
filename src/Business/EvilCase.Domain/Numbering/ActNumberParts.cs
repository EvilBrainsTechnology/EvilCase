namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// What an act number says: the case number it was issued under, its day and the sequence inside it.
/// </summary>
public sealed record ActNumberParts(string CaseNumber, DateOnly Date, int Sequence);
