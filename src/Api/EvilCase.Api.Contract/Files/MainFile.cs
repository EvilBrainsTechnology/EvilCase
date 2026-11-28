namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// The main file of an act, as the act list names it.
/// </summary>
public sealed record MainFile
{
    public required Guid FileId { get; init; }

    public required string FileName { get; init; }
}
