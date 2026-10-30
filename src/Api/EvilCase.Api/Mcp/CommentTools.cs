using System.ComponentModel;
using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Business.Comments;
using ModelContextProtocol.Server;

namespace EvilBrains.EvilCase.Api.Mcp;

[McpServerToolType]
internal static class CommentTools
{
    private const string Written = "The comment was saved.";

    [McpServerTool(Name = "list_case_comments", ReadOnly = true, OpenWorld = false)]
    [Description("Lists the comments of a case, oldest first; isAuthor says which ones the token's user wrote.")]
    public static async Task<CommentListResponse> ListCaseComments(ICommentReader comments, [Description("The case.")] Guid caseId, CancellationToken token)
    {
        var items = await comments.ListCaseComments(caseId, token) ?? throw McpProblem.Of(CaseProblems.NotFound);

        return new CommentListResponse { Items = items };
    }

    [McpServerTool(Name = "list_act_comments", ReadOnly = true, OpenWorld = false)]
    [Description("Lists the comments of an act, oldest first; isAuthor says which ones the token's user wrote.")]
    public static async Task<CommentListResponse> ListActComments(ICommentReader comments, [Description("The act.")] Guid actId, CancellationToken token)
    {
        var items = await comments.ListActComments(actId, token) ?? throw McpProblem.Of(ActProblems.ActNotFound);

        return new CommentListResponse { Items = items };
    }

    [McpServerTool(Name = "add_case_comment", Destructive = false, OpenWorld = false)]
    [Description("Adds a comment to a case under the token's user. body is required.")]
    public static async Task<string> AddCaseComment(
        ICommentWriter writer, [Description("The case.")] Guid caseId, [Description("The comment.")] CommentEditRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        var outcome = await writer.AddCaseComment(caseId, request, token);

        return outcome switch
        {
            CommentWriteOutcome.Written => Written,
            CommentWriteOutcome.NotFound => throw McpProblem.Of(CaseProblems.NotFound),
            CommentWriteOutcome.NotAuthor => throw new UnreachableException(),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "add_act_comment", Destructive = false, OpenWorld = false)]
    [Description("Adds a comment to an act under the token's user. body is required.")]
    public static async Task<string> AddActComment(
        ICommentWriter writer, [Description("The act.")] Guid actId, [Description("The comment.")] CommentEditRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        var outcome = await writer.AddActComment(actId, request, token);

        return outcome switch
        {
            CommentWriteOutcome.Written => Written,
            CommentWriteOutcome.NotFound => throw McpProblem.Of(ActProblems.ActNotFound),
            CommentWriteOutcome.NotAuthor => throw new UnreachableException(),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "update_case_comment", Destructive = false, OpenWorld = false)]
    [Description("Rewrites a comment of a case; only its author may. body is required.")]
    public static async Task<string> UpdateCaseComment(
        ICommentWriter writer,
        [Description("The case.")] Guid caseId,
        [Description("The comment.")] Guid commentId,
        [Description("The comment's new text.")] CommentEditRequest request,
        CancellationToken token)
    {
        McpProblem.Validate(request);

        return Answer(await writer.UpdateCaseComment(caseId, commentId, request, token));
    }

    [McpServerTool(Name = "update_act_comment", Destructive = false, OpenWorld = false)]
    [Description("Rewrites a comment of an act; only its author may. body is required.")]
    public static async Task<string> UpdateActComment(
        ICommentWriter writer,
        [Description("The act.")] Guid actId,
        [Description("The comment.")] Guid commentId,
        [Description("The comment's new text.")] CommentEditRequest request,
        CancellationToken token)
    {
        McpProblem.Validate(request);

        return Answer(await writer.UpdateActComment(actId, commentId, request, token));
    }

    [McpServerTool(Name = "delete_case_comment", Destructive = true, OpenWorld = false)]
    [Description("Deletes a comment of a case; only its author may. Cannot be undone.")]
    public static async Task<string> DeleteCaseComment(
        ICommentWriter writer, [Description("The case.")] Guid caseId, [Description("The comment.")] Guid commentId, CancellationToken token)
    {
        return Answer(await writer.DeleteCaseComment(caseId, commentId, token));
    }

    [McpServerTool(Name = "delete_act_comment", Destructive = true, OpenWorld = false)]
    [Description("Deletes a comment of an act; only its author may. Cannot be undone.")]
    public static async Task<string> DeleteActComment(
        ICommentWriter writer, [Description("The act.")] Guid actId, [Description("The comment.")] Guid commentId, CancellationToken token)
    {
        return Answer(await writer.DeleteActComment(actId, commentId, token));
    }

    private static string Answer(CommentWriteOutcome outcome)
    {
        return outcome switch
        {
            CommentWriteOutcome.Written => Written,
            CommentWriteOutcome.NotFound => throw McpProblem.Of("Comment not found"),
            CommentWriteOutcome.NotAuthor => throw McpProblem.Of("Not the author", "Only the author edits or deletes a comment."),
            _ => throw new UnreachableException(),
        };
    }
}
