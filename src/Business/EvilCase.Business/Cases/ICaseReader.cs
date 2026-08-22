using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Reads cases for the screens that show them.
/// </summary>
public interface ICaseReader
{
    public Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Null where the id names no case of the tenant.
    /// </summary>
    public Task<CaseDetail?> Detail(Guid id, CancellationToken cancellationToken = default);
}
