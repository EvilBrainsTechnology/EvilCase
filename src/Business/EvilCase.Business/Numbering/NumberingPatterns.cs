namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// The two patterns in force, as the issuer needs them.
/// </summary>
internal sealed record NumberingPatterns(string CaseNumberPattern, string ActNumberPattern);
