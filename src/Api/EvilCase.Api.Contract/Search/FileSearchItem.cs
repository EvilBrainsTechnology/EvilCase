namespace EvilBrains.EvilCase.Api.Contract.Search;

public sealed record FileSearchItem
{
    public required Guid FileId { get; init; }

    public required string FileName { get; init; }

    public required long SizeBytes { get; init; }

    public required Guid ActId { get; init; }

    public required string ActTitle { get; init; }
}
