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
    /// Null for a pattern that can be used. The Settings screen calls this before it saves; the issuer
    /// calls it before it writes, because a pattern that got past the screen would reissue silently.
    /// </summary>
    public static NumberPatternError? Validate(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var rest = Placeholders.Aggregate(pattern, (text, placeholder) => text.Replace(placeholder, "", StringComparison.Ordinal));
        if (rest.Contains('{', StringComparison.Ordinal) || rest.Contains('}', StringComparison.Ordinal))
            return NumberPatternError.UnknownPlaceholder;

        if (!pattern.Contains(Sequence, StringComparison.Ordinal))
            return NumberPatternError.NoSequence;

        if (Names(pattern, Day) && !(Names(pattern, Month) && Names(pattern, Year)))
            return NumberPatternError.RepeatingPeriod;

        if (Names(pattern, Month) && !Names(pattern, Year))
            return NumberPatternError.RepeatingPeriod;

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
    /// The sequence is padded to three digits so numbers of one series sort as text.
    /// </summary>
    public static string Format(string pattern, in DateOnly date, int sequence, string? caseNumber = null)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        return pattern
            .Replace(CaseNumber, caseNumber ?? "", StringComparison.Ordinal)
            .Replace(Year, date.ToString("yyyy", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace(Month, date.ToString("MM", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace(Day, date.ToString("dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace(Sequence, sequence.ToString("D3", CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private static bool Names(string pattern, string placeholder) => pattern.Contains(placeholder, StringComparison.Ordinal);
}
