namespace EvilBrains.EvilCase.Domain.Numbering;

public static class NumberTail
{
    public const int SequenceDigits = 3;

    private const string DateFormat = "yyyyMMdd";

    private const int DateLength = 8;

    public static string Prefix(in DateOnly date)
    {
        return $"{date.ToString(DateFormat, CultureInfo.InvariantCulture)}-";
    }

    public static string Compose(in DateOnly date, int sequence)
    {
        return Prefix(date) + sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture);
    }

    public static NumberTailParts Parse(string tail)
    {
        return ParseOrDefault(tail) ?? throw new FormatException($"'{tail}' is not a number tail.");
    }

    public static NumberTailParts? ParseOrDefault(string? tail)
    {
        if (string.IsNullOrWhiteSpace(tail) || tail.Length < DateLength + 1 + SequenceDigits || tail[DateLength] != '-')
            return null;

        if (!DateOnly.TryParseExact(tail[..DateLength], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;

        if (!int.TryParse(tail[(DateLength + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence))
            return null;

        return string.Equals(Compose(date, sequence), tail, StringComparison.Ordinal)
            ? new NumberTailParts(date, sequence)
            : null;
    }
}
