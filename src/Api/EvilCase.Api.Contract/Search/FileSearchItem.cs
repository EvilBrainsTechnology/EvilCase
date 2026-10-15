namespace EvilBrains.EvilCase.Api.Contract.Search;

public sealed record FileSearchItem
{
    public required Guid FileId { get; init; }

    public required string FileName { get; init; }

    public required long SizeBytes { get; init; }

    /// <summary>
    /// Exactly one of CaseId and ActId is set: the owner the file hangs on.
    /// </summary>
    public Guid? CaseId { get; init; }

    public Guid? ActId { get; init; }

    public required string OwnerTitle { get; init; }
}
