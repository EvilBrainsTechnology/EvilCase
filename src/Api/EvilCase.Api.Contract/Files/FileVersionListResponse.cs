namespace EvilBrains.EvilCase.Api.Contract.Files;

public sealed record FileVersionListResponse
{
    public required IReadOnlyList<FileVersionItem> Items { get; init; }
}
