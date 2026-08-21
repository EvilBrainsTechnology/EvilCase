namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// The act's own number, <c>{case-number}/{yyyyMMdd}-{seq:3}</c> (SDD-008).
/// </summary>
public static class ActNumberFormat
{
    public static string DayPrefix(string caseNumber, in DateOnly date) => $"{caseNumber}/{NumberTail.DayPrefix(date)}";

    public static string Compose(string caseNumber, in DateOnly date, int sequence) =>
        $"{caseNumber}/{NumberTail.Compose(date, sequence)}";

    /// <summary>
    /// Throws <see cref="FormatException"/> where the value is not an act number.
    /// </summary>
    public static ActNumberParts Parse(string value) =>
        ParseOrDefault(value) ?? throw new FormatException($"'{value}' is not an act number.");

    /// <summary>
    /// Null where the value is not an act number.
    /// </summary>
    public static ActNumberParts? ParseOrDefault(string? value)
    {
        // A case number carries slashes of its own, so the act's day and sequence are what stands after the last one.
        var separator = value?.LastIndexOf('/') ?? -1;

        if (value is null || separator <= 0)
            return null;

        return NumberTail.Parse(value[(separator + 1)..]) is { } tail
            ? new ActNumberParts(value[..separator], tail.Date, tail.Sequence)
            : null;
    }

    /// <summary>
    /// The day's next free sequence under this case number. A value outside the format does not count.
    /// </summary>
    public static int NextSequence(string caseNumber, DateOnly date, IEnumerable<string> numbers) => numbers
        .Select(ParseOrDefault)
        .OfType<ActNumberParts>()
        .Where(parts => string.Equals(parts.CaseNumber, caseNumber, StringComparison.Ordinal) && parts.Date == date)
        .Select(parts => parts.Sequence)
        .DefaultIfEmpty(0)
        .Max()
        + 1;
}
