using System.Text.RegularExpressions;

namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// One series of issued numbers as it can be read back out of the column it was written into: the
/// sequence, and the text standing on either side of it. A number of another period or another shape is
/// simply not one of these.
/// </summary>
internal sealed class NumberSeries
{
    /// <summary>
    /// The character <see cref="LikePrefix"/> escapes with, and the one the <c>LIKE</c> reading by it
    /// has to name.
    /// </summary>
    public const string LikeEscape = "\\";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    private readonly Regex expression;

    /// <summary>
    /// The literal runs on each side of the sequence, with anything at all standing between two runs of
    /// one side — the case number an act number carries is whatever its case was called when the act
    /// was numbered. The two anchors are <c>\A</c> and <c>\z</c>: <c>$</c> would read a number with a
    /// newline on the end as one the pattern had written.
    /// </summary>
    public NumberSeries(IReadOnlyList<string> before, int width, IReadOnlyList<string> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        this.Prefix = before[0];
        this.expression = new Regex(
            string.Create(CultureInfo.InvariantCulture, $@"\A{Runs(before)}(?<seq>\d{{{width},}}){Runs(after)}\z"),
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
            Timeout);
    }

    /// <summary>
    /// What every number of this series begins with, so a query reads the rows that can belong to it
    /// rather than every number the owner holds. Empty for a pattern opening with its <c>{seq}</c> or
    /// with its <c>{case-number}</c>.
    /// </summary>
    public string Prefix { get; }

    /// <summary>
    /// The same prefix for a <c>LIKE</c>. A pattern's literal text is the operator's and a case number
    /// written into it is whatever was typed, so both reach the query as text rather than as wildcards
    /// of their own.
    /// </summary>
    public string LikePrefix => Escaped(this.Prefix) + "%";

    /// <summary>
    /// The highest sequence <paramref name="numbers"/> carries, and zero where none of them belongs to
    /// this series. Whoever wrote a number counts — a mark typed in by hand in the shape the pattern
    /// writes is a number the next one must not repeat.
    /// </summary>
    public int Highest(IEnumerable<string> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        return numbers.Aggregate(0, (highest, number) => Math.Max(highest, this.Sequence(number)));
    }

    private static string Runs(IReadOnlyList<string> runs) => string.Join(".*", runs.Select(Regex.Escape));

    private static string Escaped(string prefix) => prefix
        .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
        .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
        .Replace("_", LikeEscape + "_", StringComparison.Ordinal);

    // More digits than an int holds is not a number this application ever wrote, so it counts for
    // nothing rather than throwing.
    private int Sequence(string number)
    {
        var match = this.expression.Match(number);

        return match.Success && int.TryParse(match.Groups["seq"].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence) ? sequence : 0;
    }
}
