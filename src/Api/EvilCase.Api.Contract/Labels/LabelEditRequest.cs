using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Labels;

namespace EvilBrains.EvilCase.Api.Contract.Labels;

public sealed record LabelEditRequest
{
    [Required]
    [StringLength(64)]
    public required string Name { get; init; }

    public required LabelColor Color { get; init; }
}
