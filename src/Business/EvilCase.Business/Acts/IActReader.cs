using EvilBrains.EvilCase.Api.Contract.Acts;

namespace EvilBrains.EvilCase.Business.Acts;

public interface IActReader
{
    public Task<ActListResponse> ListActs(ActListRequest request, CancellationToken token);

    public Task<ActDetail?> GetActDetail(Guid actId, CancellationToken token);
}
