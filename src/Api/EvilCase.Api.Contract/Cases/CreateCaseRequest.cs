using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CreateCaseRequest
{
    public Guid? ParentCaseId { get; init; }

    public Guid? ContactId { get; init; }

    /// <summary>
    /// The case number is issued from this date, not the write's.
    /// </summary>
    public required DateOnly Date { get; init; }

    [Required]
    [StringLength(256)]
    public required string Title { get; init; }

    [StringLength(4000)]
    public string? Description { get; init; }
}
