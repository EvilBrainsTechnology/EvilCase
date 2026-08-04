using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Reads cases for the screens that show them.
/// </summary>
public interface ICaseReader
{
    public Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Null when no such case exists.
    /// </summary>
    public Task<CaseDetailResponse?> Detail(long id, CancellationToken cancellationToken = default);
}
