namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// The patterns an installation starts with, named by the seeded settings row and by whatever reads
/// them when that row is missing.
/// </summary>
public static class NumberingDefaults
{
    public const string CaseNumberPattern = "EC-{year}{month}{day}-{seq}";

    public const string ActNumberPattern = "{case-number}-{year}{month}{day}-{seq}";
}
