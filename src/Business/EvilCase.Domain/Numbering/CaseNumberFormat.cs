using System.Text.RegularExpressions;

namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// <c>EC/20260807-001</c>, the case number of SDD-008: composing, parsing and the next sequence of a
/// day, with no database in sight.
/// </summary>
public static partial class CaseNumberFormat
{
    public const string Prefix = "EC";

    public static string Compose(in DateOnly date, int sequence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, 1);

        return DayPrefix(date) + NumberFormat.ComposeSequence(sequence);
    }

    /// <summary>
    /// What every case number of one day starts with.
    /// </summary>
    public static string DayPrefix(in DateOnly date) => Prefix + "/" + NumberFormat.ComposeDay(date) + "-";

    public static CaseNumberParts? Parse(string? value)
    {
        if (value is null || Pattern.Match(value) is not { Success: true } match)
            return null;

        if (!NumberFormat.TryReadDay(match.Groups["day"].Value, out var date))
            return null;

        if (!NumberFormat.TryReadSequence(match.Groups["sequence"].Value, out var sequence))
            return null;

        return new CaseNumberParts(date, sequence);
    }

    public static bool IsValid(string? value) => Parse(value) is not null;

    /// <summary>
    /// One past the day's highest sequence. A hand-written value outside the format counts for nothing.
    /// </summary>
    public static int NextSequence(in DateOnly date, IEnumerable<string> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        var highest = 0;

        foreach (var number in numbers)
        {
            if (Parse(number) is { } parts && parts.Date == date && parts.Sequence > highest)
                highest = parts.Sequence;
        }

        return highest + 1;
    }

    [GeneratedRegex(
        "^" + Prefix + @"/(?<day>[0-9]{8})-(?<sequence>[0-9]{3,9})$",
        RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex Pattern { get; }
}
