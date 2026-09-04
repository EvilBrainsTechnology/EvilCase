namespace EvilBrains.EvilCase.Business.Files;

public sealed record FileUpload
{
    public required string FileName { get; init; }

    /// <summary>
    /// Never derived from the file extension (SDD-012).
    /// </summary>
    public required string? MediaType { get; init; }

    public required Stream Content { get; init; }
}
