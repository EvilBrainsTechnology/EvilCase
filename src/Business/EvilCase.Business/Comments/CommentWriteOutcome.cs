namespace EvilBrains.EvilCase.Business.Comments;

/// <summary>
/// How a write aimed at one note ended.
/// </summary>
public enum CommentWriteOutcome
{
    Written = 0,

    /// <summary>
    /// The note is not there, or an add found no case or act to hang it on.
    /// </summary>
    NotFound = 1,

    NotAuthor = 2,
}
