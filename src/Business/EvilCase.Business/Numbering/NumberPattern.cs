using System.Text.RegularExpressions;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// The pattern language behind an issued number: <c>{year}</c>, <c>{month}</c>, <c>{day}</c>,
/// <c>{seq}</c> — <c>{seq:6}</c> for a width of its own — and, for an act number,
/// <c>{case-number}</c>. Anything else is written out as it stands.
/// </summary>
internal static partial class NumberPattern
{
    private const string Year = "{year}";

    private const string Month = "{month}";

    private const string Day = "{day}";

    private const string CaseNumber = "{case-number}";

    /// <summary>
    /// What a <c>{seq}</c> naming no width of its own is padded to. The width is a minimum: a series
    /// past it keeps counting rather than being capped, and sorts as text up to it and no further.
    /// </summary>
    private const int DefaultWidth = 3;

    private static readonly string[] Placeholders = [Year, Month, Day, CaseNumber];

    /// <summary>
    /// The widest a placeholder ever writes: the last date <see cref="DateOnly"/> holds, and a series
    /// that has counted to <see cref="int.MaxValue"/> — ten digits, or the width if that is wider.
    /// </summary>
    private static readonly DateOnly WidestDate = DateOnly.MaxValue;

    private static readonly string WidestCaseNumber = new('9', Case.CaseNumberLength);

    /// <summary>
    /// Null for a pattern that can be used. The issuer calls it before it writes, and the API answers a
    /// screen that edits a pattern with it — a pattern that got past the screen would reissue silently,
    /// or fail on the insert.
    /// </summary>
    public static NumberPatternError? Validate(string pattern, NumberPatternKind kind)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var rest = Placeholders.Aggregate(SequenceToken.Replace(pattern, ""), (text, placeholder) => text.Replace(placeholder, "", StringComparison.Ordinal));
        if (rest.Contains('{', StringComparison.Ordinal) || rest.Contains('}', StringComparison.Ordinal))
            return NumberPatternError.UnknownPlaceholder;

        var sequences = SequenceToken.Matches(pattern);
        if (sequences.Count == 0)
            return NumberPatternError.NoSequence;

        // Two of them and the digits of one run into the digits of the other, so no reading of the
        // number tells them apart again.
        if (sequences.Count > 1)
            return NumberPatternError.RepeatedSequence;

        if (Width(sequences[0]) is not { } width)
            return NumberPatternError.SequenceWidth;

        if (kind is not NumberPatternKind.ActNumber && Names(pattern, CaseNumber))
            return NumberPatternError.CaseNumberOutsideAnActPattern;

        if (RunsIntoTheSequence(pattern, sequences[0]))
            return NumberPatternError.CaseNumberBesideTheSequence;

        if (Names(pattern, Day) && !(Names(pattern, Month) && Names(pattern, Year)))
            return NumberPatternError.RepeatingPeriod;

        if (Names(pattern, Month) && !Names(pattern, Year))
            return NumberPatternError.RepeatingPeriod;

        if (width > Column(kind) || Widest(pattern, kind) > Column(kind))
            return NumberPatternError.TooLongForItsColumn;

        return null;
    }

    /// <summary>
    /// The case number is written last: it is the operator's to type, so it may carry pattern text of
    /// its own, and no other placeholder occurs inside <c>{case-number}</c> for an earlier pass to break.
    /// </summary>
    public static string Format(string pattern, in DateOnly date, int sequence, string? caseNumber = null)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var value = sequence;

        return Literal(SequenceToken.Replace(pattern, token => Pad(value, token)), date)
            .Replace(CaseNumber, caseNumber ?? "", StringComparison.Ordinal);
    }

    /// <summary>
    /// The same pattern read the other way round: what one series of numbers already stored looks like,
    /// for the day a number is being issued on. The date is literal text by then, so a number's
    /// sequence starts and ends at a fixed distance from its edges; the case number is not, because a
    /// case renamed since leaves its acts numbered under what it was called then. The pattern is
    /// <see cref="Validate">validated</see> first — this reads the one <c>{seq}</c>.
    /// </summary>
    public static NumberSeries Series(string pattern, in DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var token = SequenceToken.Match(pattern);
        var width = Width(token) ?? DefaultWidth;
        var day = date;

        return new NumberSeries(Runs(pattern[..token.Index], day), width, Runs(pattern[(token.Index + token.Length)..], day));
    }

    [GeneratedRegex(@"\{seq(?::(?<width>[^{}]*))?\}", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SequenceToken { get; }

    /// <summary>
    /// The width the token names, <see cref="DefaultWidth"/> for one naming none, and null for one
    /// naming something that is not a positive number.
    /// </summary>
    private static int? Width(Match token) => token.Groups["width"] switch
    {
        { Success: false } => DefaultWidth,
        { ValueSpan: var named } when int.TryParse(named, NumberStyles.None, CultureInfo.InvariantCulture, out var width) && width > 0 => width,
        _ => null,
    };

    private static string Pad(int sequence, Match token) =>
        sequence.ToString(string.Create(CultureInfo.InvariantCulture, $"D{Width(token) ?? DefaultWidth}"), CultureInfo.InvariantCulture);

    private static string Literal(string text, in DateOnly date) => text
        .Replace(Year, date.ToString("yyyy", CultureInfo.InvariantCulture), StringComparison.Ordinal)
        .Replace(Month, date.ToString("MM", CultureInfo.InvariantCulture), StringComparison.Ordinal)
        .Replace(Day, date.ToString("dd", CultureInfo.InvariantCulture), StringComparison.Ordinal);

    private static IReadOnlyList<string> Runs(string text, DateOnly date) =>
        [.. text.Split(CaseNumber, StringSplitOptions.None).Select(part => Literal(part, date))];

    private static bool Names(string pattern, string placeholder) => pattern.Contains(placeholder, StringComparison.Ordinal);

    /// <summary>
    /// The case number is read back as anything at all, so what tells it apart from the sequence beside
    /// it is the text between the two. A date part writes digits, so a separator of nothing but digits
    /// is no separator at all: the two runs of digits meet and neither end can be found again.
    /// </summary>
    private static bool RunsIntoTheSequence(string pattern, Match token)
    {
        var head = pattern[..token.Index];
        var tail = pattern[(token.Index + token.Length)..];

        var before = head.LastIndexOf(CaseNumber, StringComparison.Ordinal);
        var after = tail.IndexOf(CaseNumber, StringComparison.Ordinal);

        return (before >= 0 && Digits(head[(before + CaseNumber.Length)..]))
            || (after >= 0 && Digits(tail[..after]));
    }

    private static bool Digits(string between) => Literal(between, WidestDate).All(char.IsAsciiDigit);

    private static int Column(NumberPatternKind kind) =>
        kind is NumberPatternKind.ActNumber ? Act.ActNumberLength : Case.CaseNumberLength;

    private static int Widest(string pattern, NumberPatternKind kind) =>
        Format(pattern, WidestDate, int.MaxValue, kind is NumberPatternKind.ActNumber ? WidestCaseNumber : null).Length;
}
