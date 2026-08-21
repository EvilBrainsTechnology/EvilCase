namespace EvilBrains.EvilCase.Business.Numbering;

internal interface ICaseNumberIssuer
{
    /// <summary>
    /// The next free case number of the day. The caller saves the case and retries a collision with a number
    /// issued at the same moment.
    /// </summary>
    public Task<string> NextCaseNumber(DateOnly date, CancellationToken cancellationToken = default);
}
