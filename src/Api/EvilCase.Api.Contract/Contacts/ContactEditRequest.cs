using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactEditRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(256)]
    public required string Name { get; init; }

    public required ContactKind Kind { get; init; }

    [MaxLength(16)]
    public string? DataBoxId { get; init; }

    [MaxLength(1024)]
    public string? Address { get; init; }
}
