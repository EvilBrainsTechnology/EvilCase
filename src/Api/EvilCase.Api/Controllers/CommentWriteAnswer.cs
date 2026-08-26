using System.Diagnostics;
using EvilBrains.EvilCase.Business.Comments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

internal static class CommentWriteAnswer
{
    public static ActionResult Answer(this ControllerBase controller, CommentWriteOutcome outcome)
    {
        return outcome switch
        {
            CommentWriteOutcome.Written => controller.NoContent(),
            CommentWriteOutcome.NotFound => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: "Comment not found"),
            CommentWriteOutcome.NotAuthor => controller.Problem(
                detail: "Only the author edits or deletes a comment.", statusCode: StatusCodes.Status403Forbidden, title: "Not the author"),
            _ => throw new UnreachableException(),
        };
    }
}
