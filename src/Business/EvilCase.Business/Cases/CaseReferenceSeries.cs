namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// The shape of a generated internal file mark: literal text with the date tokens <c>YYYY</c>,
/// <c>MM</c> and <c>DD</c> and a run of <c>X</c> setting the counter's minimum width.
/// <c>ECYYYYMMDD-XXX</c> gives <c>EC20260802-001</c>.
/// </summary>
public static class CaseReferenceSeries
{
    /// <summary>
    /// Everything before the counter, once the date is filled in.
    /// </summary>
    public static string Prefix(string format, in DateOnly date) => Split(format, date).Prefix;

    /// <summary>
    /// One mark. A counter too large for the format's width grows past it rather than truncating.
    /// </summary>
    public static string Format(string format, in DateOnly date, int counter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(counter, 1);

        var (prefix, width, suffix) = Split(format, date);

        return prefix + counter.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0') + suffix;
    }

    /// <summary>
    /// The counter inside an existing mark, or null when it is not of this series and day.
    /// </summary>
    public static int? CounterOf(string format, in DateOnly date, string reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var (prefix, _, suffix) = Split(format, date);

        if (!reference.StartsWith(prefix, StringComparison.Ordinal) || !reference.EndsWith(suffix, StringComparison.Ordinal))
            return null;

        var digits = reference[prefix.Length..(reference.Length - suffix.Length)];

        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var counter) ? counter : null;
    }

    /// <summary>
    /// One past the highest counter already used that day. Marks not matching the format are ignored.
    /// </summary>
    public static int NextCounter(string format, in DateOnly date, IEnumerable<string> taken)
    {
        ArgumentNullException.ThrowIfNull(taken);

        // An `in` parameter cannot be captured by the lambda below.
        var day = date;

        var highest = taken
            .Select(reference => CounterOf(format, day, reference))
            .Where(counter => counter is not null)
            .Max();

        return (highest ?? 0) + 1;
    }

    private static (string Prefix, int Width, string Suffix) Split(string format, in DateOnly date)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        var expanded = format
            .Replace("YYYY", date.Year.ToString("D4", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("MM", date.Month.ToString("D2", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("DD", date.Day.ToString("D2", CultureInfo.InvariantCulture), StringComparison.Ordinal);

        var start = expanded.IndexOf('X', StringComparison.Ordinal);

        if (start < 0)
            throw new ArgumentException("A case reference format needs a run of X for the counter.", nameof(format));

        var width = 0;

        while (start + width < expanded.Length && expanded[start + width] == 'X')
            width++;

        return (expanded[..start], width, expanded[(start + width)..]);
    }
}
