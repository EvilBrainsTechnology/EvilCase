using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Reads cases for the screens that show them.
/// </summary>
public interface ICaseReader
{
    public Task<IReadOnlyList<CaseListItem>> ListCases(CaseListRequest request, CancellationToken token);

    public Task<CaseDetail?> GetCaseDetail(Guid caseId, CancellationToken token);

    /// <summary>
    /// How many cases the tenant holds in each status, counted by the database.
    /// </summary>
    public Task<CaseStatusCounts> CountCasesByStatus(CancellationToken token);
}
