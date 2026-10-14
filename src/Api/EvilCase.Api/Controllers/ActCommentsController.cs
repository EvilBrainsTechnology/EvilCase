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
[Route("api/acts/{actId:guid}/comments")]
public class ActCommentsController : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentListResponse>> ListActComments(
        [FromServices] ICommentReader comments, [FromRoute] Guid actId, CancellationToken token)
    {
        var items = await comments.ListActComments(actId, token);

        return items is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound)
            : this.Ok(new CommentListResponse { Items = items });
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddActComment(
        [FromServices] ICommentWriter writer, [FromRoute] Guid actId, [FromBody] CommentEditRequest request, CancellationToken token)
    {
        var outcome = await writer.AddActComment(actId, request, token);

        return outcome switch
        {
            CommentWriteOutcome.Written => this.NoContent(),
            CommentWriteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: ActProblems.ActNotFound),
            CommentWriteOutcome.NotAuthor => throw new UnreachableException(),
            _ => throw new UnreachableException(),
        };
    }

    [HttpPut("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> EditActComment(
        [FromServices] ICommentWriter writer, [FromRoute] Guid actId, [FromRoute] Guid commentId, [FromBody] CommentEditRequest request, CancellationToken token)
    {
        var outcome = await writer.UpdateActComment(actId, commentId, request, token);

        return this.Answer(outcome);
    }

    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteActComment(
        [FromServices] ICommentWriter writer, [FromRoute] Guid actId, [FromRoute] Guid commentId, CancellationToken token)
    {
        var outcome = await writer.DeleteActComment(actId, commentId, token);

        return this.Answer(outcome);
    }
}
