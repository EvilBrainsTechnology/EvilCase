using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseEditRequest
{
    /// <summary>
    /// The case the new one hangs under, null for a case of its own (SDD-009).
    /// </summary>
    public Guid? ParentCaseId { get; init; }

    /// <summary>
    /// Hand-written over the issued one. The format and the tenant's uniqueness hold; the day inside it
    /// is not tied to <see cref="Date"/> (SDD-008).
    /// </summary>
    [Required]
    [StringLength(64)]
    public required string CaseNumber { get; init; }

    /// <summary>
    /// The case's own date. Moving it leaves the number as it was issued.
    /// </summary>
    public required DateOnly Date { get; init; }

    [Required]
    [StringLength(256)]
    public required string Title { get; init; }

    [StringLength(4000)]
    public string? Description { get; init; }

    public required CaseStatus Status { get; init; }
}
