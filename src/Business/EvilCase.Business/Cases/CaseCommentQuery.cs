using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Shapes a case's comment thread, one composable step per rule.
/// </summary>
public static class CaseCommentQuery
{
    /// <summary>
    /// The notes on a case; a note on an act is another thread.
    /// </summary>
    public static IQueryable<Comment> OnCase(this IQueryable<Comment> comments, long caseId)
    {
        ArgumentNullException.ThrowIfNull(comments);

        return comments.Where(comment => comment.CaseId == caseId);
    }

    /// <summary>
    /// A diary is read from its last entry, the identifier breaking the tie so the order is total.
    /// </summary>
    public static IQueryable<Comment> InDiaryOrder(this IQueryable<Comment> comments)
    {
        ArgumentNullException.ThrowIfNull(comments);

        return comments
            .OrderByDescending(comment => comment.Created)
            .ThenByDescending(comment => comment.Id);
    }

    public static IQueryable<CaseComment> AsCaseComments(this IQueryable<Comment> comments)
    {
        ArgumentNullException.ThrowIfNull(comments);

        return comments.Select(comment => new CaseComment
        {
            Id = comment.Id,
            Body = comment.Body,
            AuthorEmail = comment.Author!.Email,
            Created = comment.Created,
        });
    }
}
