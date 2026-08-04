namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// The pattern language behind an issued number: <c>{year}</c>, <c>{month}</c>, <c>{day}</c>,
/// <c>{seq}</c> and, for an act number, <c>{case-number}</c>. Anything else is written out as it stands.
/// </summary>
public static class NumberPattern
{
    private const string Year = "{year}";

    private const string Month = "{month}";

    private const string Day = "{day}";

    private const string Sequence = "{seq}";

    private const string CaseNumber = "{case-number}";

    public static bool NamesSequence(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        return pattern.Contains(Sequence, StringComparison.Ordinal);
    }

    /// <summary>
    /// The period one series counts within, as the finest date part the pattern names: a day with
    /// <c>{day}</c>, a month with <c>{month}</c>, a year with <c>{year}</c>, and all of time without any
    /// of them.
    /// </summary>
    public static string PeriodKey(string pattern, in DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (pattern.Contains(Day, StringComparison.Ordinal))
            return date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        if (pattern.Contains(Month, StringComparison.Ordinal))
            return date.ToString("yyyyMM", CultureInfo.InvariantCulture);

        if (pattern.Contains(Year, StringComparison.Ordinal))
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
}
