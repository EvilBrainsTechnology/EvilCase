using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Entities;

namespace EvilBrains.EvilCase.Business.Acts;

public interface IActWriter
{
    public Task<ActCreateResult> CreateAct(CreateActRequest request, CancellationToken token);

    public Task<ActUpdateOutcome> UpdateAct(Guid actId, ActEditRequest request, CancellationToken token);

    public Task<DeleteOutcome> DeleteAct(Guid actId, CancellationToken token);
}
