namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// The case's own mark, <c>EC/{yyyyMMdd}-{seq:3}</c> (SDD-008).
/// </summary>
public static class CaseNumberFormat
{
    private const string CasePrefix = "EC/";

    public static string Prefix(in DateOnly date) => CasePrefix + NumberTail.Prefix(date);

    public static string Compose(in DateOnly date, int sequence) => CasePrefix + NumberTail.Compose(date, sequence);

    /// <summary>
    /// Throws <see cref="FormatException"/> where the value is not a case number.
    /// </summary>
    public static CaseNumberParts Parse(string value) =>
        ParseOrDefault(value) ?? throw new FormatException($"'{value}' is not a case number.");

    /// <summary>
    /// Null where the value is not a case number.
    /// </summary>
    public static CaseNumberParts? ParseOrDefault(string? value)
    {
        if (value?.StartsWith(CasePrefix, StringComparison.Ordinal) != true)
            return null;

        return NumberTail.ParseOrDefault(value[CasePrefix.Length..]) is { } tail ? new CaseNumberParts(tail.Date, tail.Sequence) : null;
    }
}
