using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Files;

public sealed record FileRenameRequest
{
    /// <summary>
    /// Only the last path segment is kept (SDD-012).
    /// </summary>
    [Required]
    public required string FileName { get; init; }
}
