namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// The highest number one series has already had written into its column, and zero for a series that
/// has none. A case number counts within its owner, an act number within its case; the period is the
/// series' own, since a number of another one does not match it.
/// </summary>
internal interface IIssuedNumberReader
{
    public Task<int> HighestCaseNumber(NumberSeries series, CancellationToken cancellationToken = default);

    public Task<int> HighestActNumber(long caseId, NumberSeries series, CancellationToken cancellationToken = default);
}
