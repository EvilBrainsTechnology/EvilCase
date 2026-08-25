using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Writes the cases the user files.
/// </summary>
public interface ICaseWriter
{
    /// <summary>
    /// The filed case, or null where the request names a parent that is no case of the tenant.
    /// </summary>
    public Task<CaseListItem?> CreateCase(CreateCaseRequest request, CancellationToken token);

    public Task<CaseUpdateOutcome> UpdateCase(Guid caseId, CaseEditRequest request, CancellationToken token);
}
