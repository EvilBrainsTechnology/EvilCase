using EvilBrains.EvilCase.Api.Contract.Timeline;

namespace EvilBrains.EvilCase.Data.Timeline;

/// <summary>
/// Reads the merged timeline of a case and, optionally, everything under it.
/// </summary>
public interface ITimelineReader
{
    /// <summary>
    /// Oldest first. Three queries whatever the depth of the tree — the descendants are found by one
    /// recursive statement rather than by walking the tree a level at a time.
    /// </summary>
    public Task<IReadOnlyList<TimelineEntry>> Read(long caseId, bool includeDescendants, CancellationToken cancellationToken = default);
}
