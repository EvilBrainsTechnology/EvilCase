using EvilBrains.EvilCase.Api.Contract.Numbers;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Adds and removes the marks other authorities gave a case (SDD-009).
/// </summary>
public interface IExternalCaseNumberWriter
{
    public Task<ExternalCaseNumberOutcome> AddExternalCaseNumber(Guid caseId, ExternalNumberRequest request, CancellationToken token);

    /// <summary>
    /// True where the mark was the case's and is now gone.
    /// </summary>
    public Task<bool> DeleteExternalCaseNumber(Guid caseId, Guid numberId, CancellationToken token);
}
