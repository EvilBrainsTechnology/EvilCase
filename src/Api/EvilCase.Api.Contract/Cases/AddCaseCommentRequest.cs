using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record AddCaseCommentRequest
{
    [Required(AllowEmptyStrings = false)]
    public required string Body { get; init; }
}
