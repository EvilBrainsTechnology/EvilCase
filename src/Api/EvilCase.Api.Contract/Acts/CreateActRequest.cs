using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record CreateActRequest
{
    public required ActDirection Direction { get; init; }

    /// <summary>
    /// The act's own date, not the moment the row is written; the act number is issued to it.
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
