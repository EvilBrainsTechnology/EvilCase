using EvilBrains.EvilCase.Api.Contract.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Timeline;

/// <summary>
/// One thing that happened, in a sequence merged across a case and all its descendants.
/// </summary>
public sealed record TimelineEntry
{
    public required TimelineEntryKind Kind { get; init; }

    /// <summary>
    /// The row this came from, within its kind.
    /// </summary>
    public required long SourceId { get; init; }

    /// <summary>
    /// When it happened. Null where an act carries none of its dates — such an entry sorts last rather
    /// than being dropped, because a document with no date is still in the file.
    /// </summary>
    public required DateOnly? OccurredOn { get; init; }

    public required string Title { get; init; }

    /// <summary>
    /// Which case it came from. The whole point of the merged view is that this is not always the case
    /// being looked at.
    /// </summary>
    public required long CaseId { get; init; }

    public required string CaseTitle { get; init; }

    /// <summary>
    /// Set on an act, null on a comment.
    /// </summary>
    public ActDirection? Direction { get; init; }
}
