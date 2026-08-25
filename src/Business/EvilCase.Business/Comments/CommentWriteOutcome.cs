namespace EvilBrains.EvilCase.Business.Comments;

/// <summary>
/// How a write aimed at one note ended.
/// </summary>
public enum CommentWriteOutcome
{
    Written = 0,

    NotFound = 1,

    NotAuthor = 2,
}
