namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// The day-and-sequence tail every number of SDD-008 shares: <c>yyyyMMdd-nnn</c>.
/// </summary>
public static class NumberTail
{
    public const int SequenceDigits = 3;

    private const string DateFormat = "yyyyMMdd";

    private const int DateLength = 8;

    public static string Prefix(in DateOnly date) => $"{date.ToString(DateFormat, CultureInfo.InvariantCulture)}-";

    public static string Compose(in DateOnly date, int sequence) =>
        Prefix(date) + sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture);

    /// <summary>
    /// Throws <see cref="FormatException"/> where the value is not exactly "yyyyMMdd-nnn".
    /// </summary>
    public static NumberTailParts Parse(string tail) =>
        ParseOrDefault(tail) ?? throw new FormatException($"'{tail}' is not a number tail.");

    /// <summary>
    /// Null where the value is not exactly "yyyyMMdd-nnn".
    /// </summary>
    public static NumberTailParts? ParseOrDefault(string? tail)
    {
        if (string.IsNullOrWhiteSpace(tail) || tail.Length < DateLength + 1 + SequenceDigits || tail[DateLength] != '-')
            return null;

        if (!DateOnly.TryParseExact(tail[..DateLength], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;

        if (!int.TryParse(tail[(DateLength + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence))
            return null;

        return string.Equals(Compose(date, sequence), tail, StringComparison.Ordinal)
            ? new NumberTailParts { Date = date, Sequence = sequence }
            : null;
    }
}
