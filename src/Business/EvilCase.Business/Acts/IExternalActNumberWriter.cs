using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Business.Entities;

namespace EvilBrains.EvilCase.Business.Acts;

/// <summary>
/// Adds and removes the reference numbers other authorities gave an act (SDD-010).
/// </summary>
public interface IExternalActNumberWriter
{
    public Task<ExternalActNumberOutcome> AddExternalActNumber(Guid caseId, Guid actId, ExternalNumberRequest request, CancellationToken token);

    public Task<DeleteOutcome> DeleteExternalActNumber(Guid caseId, Guid actId, Guid numberId, CancellationToken token);
}
