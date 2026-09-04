using EvilBrains.EvilCase.Api.Contract.Acts;

namespace EvilBrains.EvilCase.Business.Acts;

public interface IActReader
{
    public Task<IReadOnlyList<ActListItem>> ListActs(ActListRequest request, CancellationToken token);

    public Task<IReadOnlyList<ActListItem>> ListCaseActs(Guid caseId, CancellationToken token);

    public Task<ActDetail?> GetActDetail(Guid caseId, Guid actId, CancellationToken token);
}
