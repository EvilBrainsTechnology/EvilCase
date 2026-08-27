using EvilBrains.EvilCase.Api.Contract.Acts;

namespace EvilBrains.EvilCase.Business.Acts;

/// <summary>
/// Reads the acts a case holds.
/// </summary>
public interface IActReader
{
    public Task<IReadOnlyList<ActListItem>> ListActs(Guid caseId, CancellationToken token);

    public Task<ActDetail?> GetActDetail(Guid caseId, Guid actId, CancellationToken token);

    /// <summary>
    /// The tenant's acts across every case, newest act date first.
    /// </summary>
    public Task<IReadOnlyList<ActListItem>> ListTenantActs(ActListRequest request, CancellationToken token);
}
