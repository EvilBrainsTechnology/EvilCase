namespace EvilBrains.EvilCase.Business.Files;

public sealed record FileDownload
{
    public required string FileName { get; init; }

    public required string MediaType { get; init; }

    public required Stream Content { get; init; }
}
