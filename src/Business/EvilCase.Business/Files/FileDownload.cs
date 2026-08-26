namespace EvilBrains.EvilCase.Business.Files;

public sealed record FileDownload
{
    public required string FileName { get; init; }

    /// <summary>
    /// Non-null: the reader substitutes the default.
    /// </summary>
    public required string MediaType { get; init; }

    public required Stream Content { get; init; }
}
