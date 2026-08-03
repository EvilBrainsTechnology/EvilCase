using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Files;

internal sealed record FileStoreSettings
{
    /// <summary>
    /// Where blobs are kept. A relative path is resolved against the content root.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string RootPath { get; init; }
}
