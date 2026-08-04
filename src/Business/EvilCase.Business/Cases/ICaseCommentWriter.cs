using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Writes the case's running diary.
/// </summary>
public interface ICaseCommentWriter
{
    /// <summary>
    /// Null when no such case exists.
    /// </summary>
    public Task<CaseComment?> Add(long caseId, AddCaseCommentRequest request, CancellationToken cancellationToken = default);
}
