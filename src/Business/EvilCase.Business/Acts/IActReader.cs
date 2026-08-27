using EvilBrains.EvilCase.Api.Contract.Acts;

namespace EvilBrains.EvilCase.Business.Acts;

/// <summary>
/// Reads acts for the screens that show them.
/// </summary>
public interface IActReader
{
    /// <summary>
    /// The acts across every case, newest act date first.
    /// </summary>
    public Task<IReadOnlyList<ActListItem>> ListActs(ActListRequest request, CancellationToken token);

    public Task<IReadOnlyList<ActListItem>> ListCaseActs(Guid caseId, CancellationToken token);

    public Task<ActDetail?> GetActDetail(Guid caseId, Guid actId, CancellationToken token);
}
