using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

public interface ICaseReader
{
    public Task<IReadOnlyList<CaseListItem>> ListCases(CaseListRequest request, CancellationToken token);

    public Task<CaseDetail?> GetCaseDetail(Guid caseId, CancellationToken token);

    public Task<CaseStatusCounts> CountCasesByStatus(CancellationToken token);
}
