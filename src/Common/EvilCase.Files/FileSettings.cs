using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Files;

internal sealed record FileSettings
{
    /// <summary>
    /// The directory every blob lives under. Validated on start: a deployment that names none fails at
    /// startup rather than on the first upload. The deployed image fixes it; development names its own.
    /// </summary>
    [Required]
    public required string RootPath { get; init; }
}
