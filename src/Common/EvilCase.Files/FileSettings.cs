using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Files;

internal sealed record FileSettings
{
    [Required]
    public required string RootPath { get; init; }
}
