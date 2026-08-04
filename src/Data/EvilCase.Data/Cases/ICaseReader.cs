using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Data.Cases;

/// <summary>
/// Reads cases for the screens that show them.
/// </summary>
public interface ICaseReader
{
    public Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default);
}
