namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Why a pattern cannot be used. The screen that edits one gets it through the API and turns each of
/// these into its own sentence.
/// </summary>
internal enum NumberPatternError
{
    /// <summary>
    /// A <c>{…}</c> the application does not know, or a brace with no partner.
    /// </summary>
    UnknownPlaceholder = 0,

    /// <summary>
    /// No <c>{seq}</c>, so the pattern writes one number over and over.
    /// </summary>
    NoSequence = 1,

    /// <summary>
    /// A date part written without the coarser ones around it: <c>EC-{day}-{seq}</c> counts within a
    /// day but writes only the day of the month, so next month it writes the same numbers again.
    /// </summary>
    RepeatingPeriod = 2,

    /// <summary>
    /// <c>{case-number}</c> in a case pattern: a placeholder the application knows, in the one field
    /// that has no case number to write there.
    /// </summary>
    CaseNumberOutsideAnActPattern = 3,

    /// <summary>
    /// The number it writes at its widest — a series counted to <see cref="int.MaxValue"/>, and a
    /// case number filling its own column — does not fit the column it is stored in.
    /// </summary>
    TooLongForItsColumn = 4,

    /// <summary>
    /// More than one <c>{seq}</c>. Each is a run of digits of no fixed length, so the two run into one
    /// another and reading the number back cannot say where either ends.
    /// </summary>
    RepeatedSequence = 5,

    /// <summary>
    /// <c>{seq:…}</c> naming something that is not a positive number of digits.
    /// </summary>
    SequenceWidth = 6,

    /// <summary>
    /// Nothing but digits between the <c>{case-number}</c> and the <c>{seq}</c>. A case number is read
    /// back as anything at all, so <c>EC-20261000</c> is case <c>EC-2026</c> at 1000 and case
    /// <c>EC-2</c> at 0261000 alike, and no reading tells the two apart.
    /// </summary>
    CaseNumberBesideTheSequence = 7,
}
