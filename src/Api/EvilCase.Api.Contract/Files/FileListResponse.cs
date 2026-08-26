namespace EvilBrains.EvilCase.Api.Contract.Files;

public sealed record FileListResponse
{
    public required IReadOnlyList<FileListItem> Items { get; init; }
}
