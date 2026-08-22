namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// The case's own mark, <c>EC/{yyyyMMdd}-{seq:3}</c> (SDD-008).
/// </summary>
public static class CaseNumberFormat
{
    private const string CasePrefix = "EC/";

    public static string Prefix(in DateOnly date)
    {
        return CasePrefix + NumberTail.Prefix(date);
    }

    public static string Compose(in DateOnly date, int sequence)
    {
        return CasePrefix + NumberTail.Compose(date, sequence);
    }

    /// <summary>
    /// The number that follows the day's highest. A day without one of its own starts at 001, and so does a
    /// highest that stands outside the format.
    /// </summary>
    public static string Next(in DateOnly date, string? highest)
    {
        return Compose(date, (ParseOrDefault(highest)?.Sequence ?? 0) + 1);
    }

    /// <summary>
    /// Throws <see cref="FormatException"/> where the value is not a case number.
    /// </summary>
    public static CaseNumberParts Parse(string value)
    {
        return ParseOrDefault(value) ?? throw new FormatException($"'{value}' is not a case number.");
    }

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
