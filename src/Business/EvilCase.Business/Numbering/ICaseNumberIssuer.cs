namespace EvilBrains.EvilCase.Business.Numbering;

public interface ICaseNumberIssuer
{
    /// <summary>
    /// The next free case number of the day. The caller saves the case; a collision with a number issued at
    /// the same moment is not handled yet and comes with the writer (M3).
    /// </summary>
    public Task<string> NextCaseNumber(DateOnly date, CancellationToken cancellationToken = default);
}
