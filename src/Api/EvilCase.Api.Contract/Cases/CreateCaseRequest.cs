using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CreateCaseRequest
{
    /// <summary>
    /// The case the new one hangs under, null for a case of its own (SDD-009).
    /// </summary>
    public Guid? ParentCaseId { get; init; }

    /// <summary>
    /// The counterparty of the proceeding, null for none (SDD-009).
    /// </summary>
    public Guid? ContactId { get; init; }

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
