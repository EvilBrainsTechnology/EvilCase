namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// The act's own number, <c>{case-number}/{yyyyMMdd}-{seq:3}</c> (SDD-008).
/// </summary>
public static class ActNumberFormat
{
    public static string Prefix(string caseNumber, in DateOnly date)
    {
        return $"{caseNumber}/{NumberTail.Prefix(date)}";
    }

    public static string Compose(string caseNumber, in DateOnly date, int sequence)
    {
        return $"{caseNumber}/{NumberTail.Compose(date, sequence)}";
    }

    /// <summary>
    /// The number that follows the day's highest inside the case. A day without one of its own starts at 001,
    /// and so does a highest that stands outside the format.
    /// </summary>
    public static string Next(string caseNumber, in DateOnly date, string? highest)
    {
        return Compose(caseNumber, date, (ParseOrDefault(highest)?.Sequence ?? 0) + 1);
    }

    /// <summary>
    /// Throws <see cref="FormatException"/> where the value is not an act number.
    /// </summary>
    public static ActNumberParts Parse(string value)
    {
        return ParseOrDefault(value) ?? throw new FormatException($"'{value}' is not an act number.");
    }

    /// <summary>
    /// Null where the value is not an act number.
    /// </summary>
    public static ActNumberParts? ParseOrDefault(string? value)
    {
        // A case number carries slashes of its own, so the act's day and sequence are what stands after the last one.
        var separator = value?.LastIndexOf('/') ?? -1;

        if (value is null || separator <= 0)
            return null;

        return NumberTail.ParseOrDefault(value[(separator + 1)..]) is { } tail
            ? new ActNumberParts(value[..separator], tail.Date, tail.Sequence)
            : null;
    }
}
