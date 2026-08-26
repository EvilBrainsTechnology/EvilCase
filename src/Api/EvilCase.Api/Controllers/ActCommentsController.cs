using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Business.Comments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/cases/{caseId:guid}/acts/{actId:guid}/comments")]
public class ActCommentsController : ControllerBase
{
    [HttpGet("")]
    public async Task<CommentListResponse> ListActComments(
        [FromServices] ICommentReader comments, [FromRoute] Guid caseId, [FromRoute] Guid actId, CancellationToken token)
    {
        return new CommentListResponse { Items = await comments.ListActComments(caseId, actId, token) };
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddActComment(
        [FromServices] ICommentWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid actId, [FromBody] CommentEditRequest request, CancellationToken token)
    {
        var outcome = await writer.AddActComment(caseId, actId, request, token);

        return outcome == CommentWriteOutcome.Written
            ? this.NoContent()
            : this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound);
    }

    [HttpPut("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> EditActComment(
        [FromServices] ICommentWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid actId, [FromRoute] Guid commentId, [FromBody] CommentEditRequest request, CancellationToken token)
    {
        var outcome = await writer.UpdateActComment(caseId, actId, commentId, request, token);

        return this.Answer(outcome);
    }

    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteActComment(
        [FromServices] ICommentWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid actId, [FromRoute] Guid commentId, CancellationToken token)
    {
        var outcome = await writer.DeleteActComment(caseId, actId, commentId, token);

        return this.Answer(outcome);
    }

    private ActionResult Answer(CommentWriteOutcome outcome)
    {
        return outcome switch
        {
            CommentWriteOutcome.Written => this.NoContent(),
            CommentWriteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Comment not found"),
            CommentWriteOutcome.NotAuthor => this.Problem(
                detail: "Only the author edits or deletes a comment.", statusCode: StatusCodes.Status403Forbidden, title: "Not the author"),
            _ => throw new UnreachableException(),
        };
    }
}
