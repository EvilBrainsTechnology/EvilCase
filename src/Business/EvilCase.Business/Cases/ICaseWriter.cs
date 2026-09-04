using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Entities;

namespace EvilBrains.EvilCase.Business.Cases;

public interface ICaseWriter
{
    public Task<CaseCreateResult> CreateCase(CreateCaseRequest request, CancellationToken token);

    public Task<CaseUpdateOutcome> UpdateCase(Guid caseId, CaseEditRequest request, CancellationToken token);

    public Task<DeleteOutcome> DeleteCase(Guid caseId, CancellationToken token);
}
