namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// One page of the text read from a file version.
/// </summary>
public sealed record FileTextPage
{
    public required Guid FileId { get; init; }

    public required string FileName { get; init; }

    public required int Version { get; init; }

    /// <summary>
    /// 1-based.
    /// </summary>
    public required int Page { get; init; }

    public required int PageCount { get; init; }

    /// <summary>
    /// The characters of the whole text, every page together.
    /// </summary>
    public required int TotalLength { get; init; }

    public required string Text { get; init; }
}
