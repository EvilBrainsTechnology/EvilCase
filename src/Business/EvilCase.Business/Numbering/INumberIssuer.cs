namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Issues the numbers the application assigns itself. What somebody else assigned is free text and
/// never comes from here.
/// </summary>
public interface INumberIssuer
{
    public Task<string> IssueCaseNumber(CancellationToken cancellationToken = default);

    /// <summary>
    /// The act's number, counted within its case and written under the case's own number, whether that
    /// one was issued here or typed in by hand.
    /// </summary>
    public Task<string> IssueActNumber(long caseId, string caseNumber, CancellationToken cancellationToken = default);
}
