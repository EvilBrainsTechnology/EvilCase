using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Business.Comments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/cases/{caseId:guid}/comments")]
public class CaseCommentsController : ControllerBase
{
    [HttpGet("")]
    public async Task<CommentListResponse> ListCaseComments([FromServices] ICommentReader comments, [FromRoute] Guid caseId, CancellationToken token)
    {
        return new CommentListResponse { Items = await comments.ListCaseComments(caseId, token) };
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddCaseComment(
        [FromServices] ICommentWriter writer, [FromRoute] Guid caseId, [FromBody] CommentEditRequest request, CancellationToken token)
    {
        var added = await writer.AddCaseComment(caseId, request, token);

        return added
            ? this.NoContent()
            : this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Case not found");
    }

    [HttpPut("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> EditCaseComment(
        [FromServices] ICommentWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid commentId, [FromBody] CommentEditRequest request, CancellationToken token)
    {
        var outcome = await writer.UpdateCaseComment(caseId, commentId, request, token);

        return this.Answer(outcome);
    }

    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteCaseComment(
        [FromServices] ICommentWriter writer, [FromRoute] Guid caseId, [FromRoute] Guid commentId, CancellationToken token)
    {
        var outcome = await writer.DeleteCaseComment(caseId, commentId, token);

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
