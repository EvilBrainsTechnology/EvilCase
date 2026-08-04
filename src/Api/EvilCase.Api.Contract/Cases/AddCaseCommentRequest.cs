using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record AddCaseCommentRequest
{
    /// <summary>
    /// Generous for a diary entry, and short of what would weigh down every later read of the case. The
    /// column stays unbounded; this is what the API accepts.
    /// </summary>
    public const int BodyMaxLength = 10_000;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(BodyMaxLength)]
    public required string Body { get; init; }
}
