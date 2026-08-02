using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Files;

internal sealed record FileStoreSettings
{
    /// <summary>
    /// Where blobs are kept. A relative path is resolved against the content root, so the default works
    /// from a clone; a deployment points this at a mounted volume.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string RootPath { get; init; }
}
