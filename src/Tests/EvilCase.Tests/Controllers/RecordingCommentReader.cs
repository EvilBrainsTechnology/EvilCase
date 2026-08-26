using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Business.Comments;

namespace EvilBrains.EvilCase.Tests.Controllers;

internal sealed class RecordingCommentReader : ICommentReader
{
    public Guid? CaseId { get; private set; }

    public Guid? ActId { get; private set; }

    public IReadOnlyList<CommentItem> Items { get; init; } = [];

    public Task<IReadOnlyList<CommentItem>> ListCaseComments(Guid caseId, CancellationToken token)
    {
        this.CaseId = caseId;

        return Task.FromResult(this.Items);
    }

    public Task<IReadOnlyList<CommentItem>> ListActComments(Guid caseId, Guid actId, CancellationToken token)
    {
        this.CaseId = caseId;
        this.ActId = actId;

        return Task.FromResult(this.Items);
    }
}
