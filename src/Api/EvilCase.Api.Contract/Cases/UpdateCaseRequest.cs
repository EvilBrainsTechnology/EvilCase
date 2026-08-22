using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record UpdateCaseRequest
{
    /// <summary>
    /// Overwritten by hand; the format and the uniqueness are the business layer's (SDD-008).
    /// </summary>
    [Required]
    [StringLength(64)]
    public required string CaseNumber { get; init; }

    public required DateOnly Date { get; init; }

    [Required]
    [StringLength(256)]
    public required string Title { get; init; }

    [StringLength(4000)]
    public string? Description { get; init; }

    [EnumDataType(typeof(CaseStatus))]
    public required CaseStatus Status { get; init; }
}
