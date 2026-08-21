using System.Text.RegularExpressions;

namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// <c>EC/20260807-001/20260812-001</c>, the act number of SDD-008: composing, parsing and the next
/// sequence of a day, with no database in sight.
/// </summary>
public static partial class ActNumberFormat
{
    public static string Compose(string caseNumber, in DateOnly date, int sequence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, 1);

        if (!CaseNumberFormat.IsValid(caseNumber))
            throw new ArgumentException("The case number is outside the format.", nameof(caseNumber));

        return DayPrefix(caseNumber, date) + NumberFormat.ComposeSequence(sequence);
    }

    /// <summary>
    /// What every act number of the case's day starts with.
    /// </summary>
    public static string DayPrefix(string caseNumber, in DateOnly date) =>
        caseNumber + "/" + NumberFormat.ComposeDay(date) + "-";

    public static ActNumberParts? Parse(string? value)
    {
        if (value is null || Pattern.Match(value) is not { Success: true } match)
            return null;

        if (CaseNumberFormat.Parse(match.Groups["case"].Value) is null)
            return null;

        if (!NumberFormat.TryReadDay(match.Groups["day"].Value, out var date))
            return null;

        if (!NumberFormat.TryReadSequence(match.Groups["sequence"].Value, out var sequence))
            return null;

        return new ActNumberParts(match.Groups["case"].Value, date, sequence);
    }

    public static bool IsValid(string? value) => Parse(value) is not null;

    /// <summary>
    /// One past the day's highest sequence among the case's numbers, whatever case number they carry — a
    /// rewritten case number leaves the numbers it already issued alone.
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
        "^(?<case>" + CaseNumberFormat.Prefix + @"/[0-9]{8}-[0-9]{3,9})/(?<day>[0-9]{8})-(?<sequence>[0-9]{3,9})$",
        RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex Pattern { get; }
}
