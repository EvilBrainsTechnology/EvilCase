using EvilBrains.EvilCase.Api.Contract.Numbers;

namespace EvilBrains.EvilCase.Business.Acts;

/// <summary>
/// Adds and removes the reference numbers other authorities gave an act (SDD-010).
/// </summary>
public interface IExternalActNumberWriter
{
    public Task<ExternalActNumberOutcome> AddExternalActNumber(Guid caseId, Guid actId, ExternalNumberRequest request, CancellationToken token);

    public Task<ExternalActNumberDeleteOutcome> DeleteExternalActNumber(Guid caseId, Guid actId, Guid numberId, CancellationToken token);
}
