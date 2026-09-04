using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Entities;

namespace EvilBrains.EvilCase.Business.Acts;

public interface IActWriter
{
    public Task<ActCreateResult> CreateAct(Guid caseId, CreateActRequest request, CancellationToken token);

    public Task<ActUpdateOutcome> UpdateAct(Guid caseId, Guid actId, ActEditRequest request, CancellationToken token);

    public Task<DeleteOutcome> DeleteAct(Guid caseId, Guid actId, CancellationToken token);
}
