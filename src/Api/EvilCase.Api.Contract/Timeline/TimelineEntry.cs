using EvilBrains.EvilCase.Domain.Acts;

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
    /// Null where an act carries none of its dates; such an entry sorts last.
    /// </summary>
    public required DateOnly? OccurredOn { get; init; }

    public required string Title { get; init; }

    /// <summary>
    /// Which case it came from, which in a merged view is not always the one being looked at.
    /// </summary>
    public required long CaseId { get; init; }

    public required string CaseTitle { get; init; }

    /// <summary>
    /// Set on an act, null on a comment.
    /// </summary>
    public ActDirection? Direction { get; init; }
}
