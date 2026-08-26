using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Writes the cases the user files.
/// </summary>
public interface ICaseWriter
{
    public Task<CaseCreateResult> CreateCase(CreateCaseRequest request, CancellationToken token);

    public Task<CaseUpdateOutcome> UpdateCase(Guid caseId, CaseEditRequest request, CancellationToken token);

    public Task<CaseDeleteOutcome> DeleteCase(Guid caseId, CancellationToken token);
}
