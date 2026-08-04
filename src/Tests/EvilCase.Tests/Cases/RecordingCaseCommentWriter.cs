using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;

namespace EvilBrains.EvilCase.Tests.Cases;

internal sealed class RecordingCaseCommentWriter : ICaseCommentWriter
{
    public long? CaseId { get; private set; }

    public AddCaseCommentRequest? Request { get; private set; }

    public CaseComment? Comment { get; init; }

    public Task<CaseComment?> Add(long caseId, AddCaseCommentRequest request, CancellationToken cancellationToken = default)
    {
        this.CaseId = caseId;
        this.Request = request;

        return Task.FromResult(this.Comment);
    }
}
