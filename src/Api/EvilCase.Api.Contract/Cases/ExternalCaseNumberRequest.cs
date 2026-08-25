using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record ExternalCaseNumberRequest
{
    /// <summary>
    /// The mark as whoever assigned it writes it, unique within the case (SDD-009).
    /// </summary>
    [Required]
    [StringLength(128)]
    public required string Value { get; init; }

    /// <summary>
    /// The contact that assigned it; a mark nobody assigned is the case's own (SDD-009).
    /// </summary>
    public required Guid AssignedByContactId { get; init; }
}
