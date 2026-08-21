using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CreateCaseRequest
{
    /// <summary>
    /// The case's own date, not the moment the row is written; the case number is issued to it.
    /// </summary>
    public required DateOnly Date { get; init; }

    [Required]
    [StringLength(256)]
    public required string Title { get; init; }

    [StringLength(4000)]
    public string? Description { get; init; }
}
