using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Comments;

internal static class CommentListQuery
{
    public static IQueryable<Comment> OnCase(this IQueryable<Comment> comments, Guid caseId)
    {
        return comments.Where(comment => comment.CaseId == caseId);
    }

    /// <summary>
    /// The join is what carries the e-mail — <see cref="Comment"/> has no <c>User</c> navigation.
    /// </summary>
    public static IQueryable<CommentItem> AsCommentItems(this IQueryable<Comment> comments, IQueryable<User> users, Guid signedInUserId)
    {
        return comments.Join(
            users,
            comment => comment.UserId,
            user => user.Id,
            (comment, user) => new CommentItem
            {
                Id = comment.Id,
                Body = comment.Body,
                AuthorEmail = user.Email,
                IsAuthor = comment.UserId == signedInUserId,
                Created = comment.Created,
                Updated = comment.Updated,
            });
    }

    public static IQueryable<CommentItem> InDiaryOrder(this IQueryable<CommentItem> items)
    {
        return items.OrderBy(item => item.Created).ThenBy(item => item.Id);
    }
}
