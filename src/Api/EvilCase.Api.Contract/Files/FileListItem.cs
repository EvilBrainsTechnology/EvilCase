namespace EvilBrains.EvilCase.Api.Contract.Files;

public sealed record FileListItem
{
    public required Guid Id { get; init; }

    public required string FileName { get; init; }

    public required long SizeBytes { get; init; }

    /// <summary>
    /// When the file was uploaded.
    /// </summary>
    public required DateTime Created { get; init; }
}
