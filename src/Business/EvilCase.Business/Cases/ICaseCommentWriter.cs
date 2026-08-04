using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Writes the case's running diary. Writing moves the case up the list.
/// </summary>
public interface ICaseCommentWriter
{
    /// <summary>
    /// Null when the caller owns no such case.
    /// </summary>
    public Task<CaseComment?> Add(long caseId, AddCaseCommentRequest request, CancellationToken cancellationToken = default);
}
