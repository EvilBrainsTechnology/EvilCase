namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// The case's own mark, <c>EC/{yyyyMMdd}-{seq:3}</c> (SDD-008).
/// </summary>
public static class CaseNumberFormat
{
    private const string Prefix = "EC/";

    public static string DayPrefix(in DateOnly date) => Prefix + NumberTail.DayPrefix(date);

    public static string Compose(in DateOnly date, int sequence) => Prefix + NumberTail.Compose(date, sequence);

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
        if (value?.StartsWith(Prefix, StringComparison.Ordinal) != true)
            return null;

        return NumberTail.Parse(value[Prefix.Length..]) is { } tail ? new CaseNumberParts(tail.Date, tail.Sequence) : null;
    }

    /// <summary>
    /// The day's next free sequence. A value outside the format, or of another day, does not count.
    /// </summary>
    public static int NextSequence(DateOnly date, IEnumerable<string> numbers) => numbers
        .Select(ParseOrDefault)
        .OfType<CaseNumberParts>()
        .Where(parts => parts.Date == date)
        .Select(parts => parts.Sequence)
        .DefaultIfEmpty(0)
        .Max()
        + 1;
}
