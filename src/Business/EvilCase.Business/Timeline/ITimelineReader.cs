using EvilBrains.EvilCase.Api.Contract.Timeline;

namespace EvilBrains.EvilCase.Business.Timeline;

/// <summary>
/// Reads the merged timeline of a case and, optionally, everything under it.
/// </summary>
public interface ITimelineReader
{
    /// <summary>
    /// Oldest first. Three queries whatever the depth of the tree.
    /// </summary>
    public Task<IReadOnlyList<TimelineEntry>> Read(long caseId, bool includeDescendants, CancellationToken cancellationToken = default);
}
