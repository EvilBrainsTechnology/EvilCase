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
}
