using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Reads cases for the screens that show them.
/// </summary>
public interface ICaseReader
{
    public Task<IReadOnlyList<CaseListItem>> ListCases(CaseListRequest request, CancellationToken token);
}
