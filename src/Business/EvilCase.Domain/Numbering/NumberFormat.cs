namespace EvilBrains.EvilCase.Domain.Numbering;

/// <summary>
/// The parts every number of SDD-008 shares: a day and a sequence inside it.
/// </summary>
internal static class NumberFormat
{
    internal const string DayPattern = "yyyyMMdd";

    /// <summary>
    /// Three digits until a day runs out of them; overflow adds a digit.
    /// </summary>
    internal const int SequenceDigits = 3;

    internal static string ComposeDay(in DateOnly date) => date.ToString(DayPattern, CultureInfo.InvariantCulture);

    internal static string ComposeSequence(int sequence) =>
        sequence.ToString(CultureInfo.InvariantCulture).PadLeft(SequenceDigits, '0');

    internal static bool TryReadDay(string text, out DateOnly date) =>
        DateOnly.TryParseExact(text, DayPattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>
    /// Only what <see cref="ComposeSequence"/> writes reads back: no zero, no padding past three digits.
    /// </summary>
    internal static bool TryReadSequence(string text, out int sequence)
    {
        sequence = 0;

        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 1)
            return false;

        if (!string.Equals(ComposeSequence(value), text, StringComparison.Ordinal))
            return false;

        sequence = value;

        return true;
    }
}
