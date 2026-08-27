using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Business.Entities;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Adds and removes the marks other authorities gave a case (SDD-009).
/// </summary>
public interface IExternalCaseNumberWriter
{
    public Task<ExternalCaseNumberOutcome> AddExternalCaseNumber(Guid caseId, ExternalNumberRequest request, CancellationToken token);

    public Task<DeleteOutcome> DeleteExternalCaseNumber(Guid caseId, Guid numberId, CancellationToken token);
}
