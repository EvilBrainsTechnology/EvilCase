namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Issues the numbers the application assigns itself. What somebody else assigned is free text and
/// never comes from here.
/// </summary>
/// <remarks>
/// A number is the one after the highest already stored, so two callers at once can build the same one
/// and the unique index refuses the second. <c>create</c> is therefore called again under the number
/// after it: it builds and saves its own row, and a refused attempt's rows are detached before the
/// next one runs.
/// </remarks>
public interface INumberIssuer
{
    public Task<T> IssueCaseNumber<T>(Func<string, CancellationToken, Task<T>> create, CancellationToken cancellationToken = default);

    /// <summary>
    /// The act's number, counted within its case and written under the case's own number, whether that
    /// one was issued here or typed in by hand. It carries the day it is issued on, so an act entered
    /// today for something that happened in July is numbered today; backdating it renumbers nothing.
    /// A case the caller does not own is <see cref="Cases.CaseNotFoundException"/>.
    /// </summary>
    public Task<T> IssueActNumber<T>(long caseId, Func<string, CancellationToken, Task<T>> create, CancellationToken cancellationToken = default);
}
