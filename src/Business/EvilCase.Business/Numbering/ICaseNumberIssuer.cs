namespace EvilBrains.EvilCase.Business.Numbering;

internal interface ICaseNumberIssuer
{
    /// <summary>
    /// The next free case number of the day.
    /// </summary>
    public Task<string> NextCaseNumber(DateOnly date, CancellationToken cancellationToken = default);
}
