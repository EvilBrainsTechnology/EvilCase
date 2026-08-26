namespace EvilBrains.EvilCase.Business.Files;

public sealed record FileUpload
{
    public required string FileName { get; init; }

    /// <summary>
    /// What the upload said the bytes are; the extension is never trusted (SDD-012).
    /// </summary>
    public required string? MediaType { get; init; }

    public required Stream Content { get; init; }
}
