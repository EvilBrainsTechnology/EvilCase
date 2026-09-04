namespace EvilBrains.EvilCase.Domain.Numbering;

public sealed record ActNumberParts(string CaseNumber, DateOnly Date, int Sequence);
