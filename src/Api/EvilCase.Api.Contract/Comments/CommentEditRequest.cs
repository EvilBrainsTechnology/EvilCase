using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Comments;

public sealed record CommentEditRequest
{
    [Required]
    public required string Body { get; init; }
}
