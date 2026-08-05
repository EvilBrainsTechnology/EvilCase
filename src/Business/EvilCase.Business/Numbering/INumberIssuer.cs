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
    /// one was issued here or typed in by hand. It carries the day it is issued on, so an act entered
    /// today for something that happened in July is numbered today; backdating it renumbers nothing.
    /// A case the caller does not own is <see cref="Cases.CaseNotFoundException"/>.
    /// </summary>
    public Task<string> IssueActNumber(long caseId, CancellationToken cancellationToken = default);
}
