namespace EvilBrains.EvilCase.Domain.Numbering;

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

    public static string Next(in DateOnly date, string? highest)
    {
        return Compose(date, (ParseOrDefault(highest)?.Sequence ?? 0) + 1);
    }

    public static CaseNumberParts Parse(string value)
    {
        return ParseOrDefault(value) ?? throw new FormatException($"'{value}' is not a case number.");
    }

    public static CaseNumberParts? ParseOrDefault(string? value)
    {
        if (value?.StartsWith(CasePrefix, StringComparison.Ordinal) != true)
            return null;

        return NumberTail.ParseOrDefault(value[CasePrefix.Length..]) is { } tail ? new CaseNumberParts(tail.Date, tail.Sequence) : null;
    }
}
