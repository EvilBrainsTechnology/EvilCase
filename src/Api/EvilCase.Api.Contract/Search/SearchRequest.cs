using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Search;

public sealed record SearchRequest
{
    /// <summary>
    /// The text to find, at least two characters once trimmed.
    /// </summary>
    [Required]
    [StringLength(SearchLimits.TermMaxLength, MinimumLength = SearchLimits.TermMinLength)]
    [RegularExpression(@"^\s*\S.*\S\s*$", ErrorMessage = "The term needs at least two characters besides surrounding spaces.")]
    public required string Q { get; init; }
}
