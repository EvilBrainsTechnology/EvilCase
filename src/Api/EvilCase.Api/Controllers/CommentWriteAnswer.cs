using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Comments;
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
            CommentWriteOutcome.NotFound => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: CommentProblems.NotFound),
            CommentWriteOutcome.NotAuthor => controller.Problem(
                detail: CommentProblems.NotAuthorDetail, statusCode: StatusCodes.Status403Forbidden, title: CommentProblems.NotAuthor),
            _ => throw new UnreachableException(),
        };
    }
}
