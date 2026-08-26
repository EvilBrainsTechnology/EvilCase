using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActEditRequest
{
    /// <summary>
    /// Hand-written over the issued one. The format and the tenant's uniqueness hold; the day inside it
    /// is not tied to <see cref="Date"/> (SDD-008).
    /// </summary>
    [Required]
    [StringLength(128)]
    public required string ActNumber { get; init; }

    public required ActDirection Direction { get; init; }

    /// <summary>
    /// The act's own date. Moving it leaves the number as it was issued.
    /// </summary>
    public required DateOnly Date { get; init; }

    [Required]
    [StringLength(256)]
    public required string Title { get; init; }

    [StringLength(4000)]
    public string? Description { get; init; }

    public required Guid IssuedByContactId { get; init; }

    public Guid? AddressedToContactId { get; init; }
}
