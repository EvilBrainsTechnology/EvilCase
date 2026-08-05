using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// The pattern language behind an issued number: <c>{year}</c>, <c>{month}</c>, <c>{day}</c>,
/// <c>{seq}</c> and, for an act number, <c>{case-number}</c>. Anything else is written out as it stands.
/// </summary>
internal static class NumberPattern
{
    private const string Year = "{year}";

    private const string Month = "{month}";

    private const string Day = "{day}";

    private const string Sequence = "{seq}";

    private const string CaseNumber = "{case-number}";

    private static readonly string[] Placeholders = [Year, Month, Day, Sequence, CaseNumber];

    /// <summary>
    /// The widest a placeholder ever writes: the last date <see cref="DateOnly"/> holds, and a series
    /// that has counted to <see cref="int.MaxValue"/>.
    /// </summary>
    private static readonly DateOnly WidestDate = DateOnly.MaxValue;

    private static readonly string WidestCaseNumber = new('9', Case.CaseNumberLength);

    /// <summary>
    /// Null for a pattern that can be used. The issuer calls it before it writes, and the API answers a
    /// screen that edits a pattern with it — a pattern that got past the screen would reissue silently,
    /// or fail on the insert with its <c>{seq}</c> already burned.
    /// </summary>
    public static NumberPatternError? Validate(string pattern, NumberPatternKind kind)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var rest = Placeholders.Aggregate(pattern, (text, placeholder) => text.Replace(placeholder, "", StringComparison.Ordinal));
        if (rest.Contains('{', StringComparison.Ordinal) || rest.Contains('}', StringComparison.Ordinal))
            return NumberPatternError.UnknownPlaceholder;

        if (kind is not NumberPatternKind.ActNumber && Names(pattern, CaseNumber))
            return NumberPatternError.CaseNumberOutsideAnActPattern;

        if (!Names(pattern, Sequence))
            return NumberPatternError.NoSequence;

        if (Names(pattern, Day) && !(Names(pattern, Month) && Names(pattern, Year)))
            return NumberPatternError.RepeatingPeriod;

        if (Names(pattern, Month) && !Names(pattern, Year))
            return NumberPatternError.RepeatingPeriod;

        if (Widest(pattern, kind) > Column(kind))
            return NumberPatternError.TooLongForItsColumn;

        return null;
    }

    /// <summary>
    /// The period one series counts within, as the finest date part the pattern names: a day with
    /// <c>{day}</c>, a month with <c>{month}</c>, a year with <c>{year}</c>, and all of time without any
    /// of them.
    /// </summary>
    public static string PeriodKey(string pattern, in DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (Names(pattern, Day))
            return date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        if (Names(pattern, Month))
            return date.ToString("yyyyMM", CultureInfo.InvariantCulture);

        if (Names(pattern, Year))
            return date.ToString("yyyy", CultureInfo.InvariantCulture);

        return "";
    }

    /// <summary>
    /// The sequence is padded to three digits so numbers of one series sort as text. The case number is
    /// written last: it is the operator's to type, so it may carry pattern text of its own, and no other
    /// placeholder occurs inside <c>{case-number}</c> for an earlier pass to break.
    /// </summary>
    public static string Format(string pattern, in DateOnly date, int sequence, string? caseNumber = null)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        return pattern
            .Replace(Year, date.ToString("yyyy", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace(Month, date.ToString("MM", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace(Day, date.ToString("dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace(Sequence, sequence.ToString("D3", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace(CaseNumber, caseNumber ?? "", StringComparison.Ordinal);
    }

    private static bool Names(string pattern, string placeholder) => pattern.Contains(placeholder, StringComparison.Ordinal);

    private static int Column(NumberPatternKind kind) =>
        kind is NumberPatternKind.ActNumber ? Act.ActNumberLength : Case.CaseNumberLength;

    private static int Widest(string pattern, NumberPatternKind kind) =>
        Format(pattern, WidestDate, int.MaxValue, kind is NumberPatternKind.ActNumber ? WidestCaseNumber : null).Length;
}
