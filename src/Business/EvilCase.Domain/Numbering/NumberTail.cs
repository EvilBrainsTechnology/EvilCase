namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// The day-and-sequence tail every number of SDD-008 shares: <c>yyyyMMdd-nnn</c>.
/// </summary>
internal static class NumberTail
{
    public const int SequenceDigits = 3;

    private const string DateFormat = "yyyyMMdd";

    private const int DateLength = 8;

    public static string DayPrefix(in DateOnly date) => $"{date.ToString(DateFormat, CultureInfo.InvariantCulture)}-";

    public static string Compose(in DateOnly date, int sequence) =>
        DayPrefix(date) + sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture);

    /// <summary>
    /// The day and the sequence of "yyyyMMdd-nnn", or null where the text is not exactly that.
    /// </summary>
    public static (DateOnly Date, int Sequence)? Parse(string tail)
    {
        if (tail.Length < DateLength + 1 + SequenceDigits || tail[DateLength] != '-')
            return null;

        if (!DateOnly.TryParseExact(tail[..DateLength], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;

        if (!int.TryParse(tail[(DateLength + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence))
            return null;

        return string.Equals(Compose(date, sequence), tail, StringComparison.Ordinal) ? (date, sequence) : null;
    }
}
