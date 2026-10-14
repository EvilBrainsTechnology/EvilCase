using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Search;

public sealed record SearchRequest
{
    /// <summary>
    /// The text to find, with no space at either end.
    /// </summary>
    [Required]
    [StringLength(SearchLimits.TermMaxLength, MinimumLength = SearchLimits.TermMinLength)]
    [RegularExpression(@"^\S.*\S$", ErrorMessage = "The term neither starts nor ends with a space.")]
    public required string Q { get; init; }
}
