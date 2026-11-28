using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

public interface ICaseReader
{
    public Task<CaseListResponse> ListCases(CaseListRequest request, CancellationToken token);

    public Task<CaseDetail?> GetCaseDetail(Guid caseId, CancellationToken token);

    /// <summary>
    /// The case with every case below it, their acts and files; null where the case is unknown.
    /// </summary>
    public Task<CaseFileTree?> GetCaseFileTree(Guid caseId, CancellationToken token);

    public Task<CaseStatusCounts> CountCasesByStatus(CancellationToken token);
}
