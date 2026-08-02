using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Data.Cases;

/// <summary>
/// Reads cases for the screens that show them. Queries only — writing a case is its own slice.
/// </summary>
public interface ICaseReader
{
    /// <summary>
    /// The case list, narrowed by <paramref name="request"/> and ordered by what was touched last.
    /// </summary>
    public Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default);
}
