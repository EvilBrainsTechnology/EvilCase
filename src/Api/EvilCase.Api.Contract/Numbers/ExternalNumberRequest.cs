using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Numbers;

public sealed record ExternalNumberRequest
{
    /// <summary>
    /// The number as whoever assigned it writes it, unique within the case or the act it hangs on
    /// (SDD-009, SDD-010).
    /// </summary>
    [Required]
    [StringLength(128)]
    public required string Value { get; init; }

    /// <summary>
    /// The contact that assigned it; a number nobody assigned is the owner's own (SDD-009, SDD-010).
    /// </summary>
    public required Guid AssignedByContactId { get; init; }
}
