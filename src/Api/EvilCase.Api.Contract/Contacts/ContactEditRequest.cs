using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactEditRequest
{
    [Required]
    [StringLength(256)]
    public required string Name { get; init; }

    public required ContactKind Kind { get; init; }

    [StringLength(16)]
    public string? DataBoxId { get; init; }

    [StringLength(1024)]
    public string? Address { get; init; }
}
