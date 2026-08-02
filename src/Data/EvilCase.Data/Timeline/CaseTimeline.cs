using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Timeline;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Data.Timeline;

/// <summary>
/// Turns the acts and comments of a case tree into one chronological sequence. Pure over what it is
/// given, so the merge and its ordering are settled here and reading the database is somebody else's
/// problem.
/// </summary>
public static class CaseTimeline
{
    /// <summary>
    /// The date an act happened, which depends on which way it travelled: an outgoing act happened when
    /// it was sent, an incoming one when it arrived. Each falls back through the dates that are nearly
    /// the same thing, and an act carrying none of them has no date at all rather than a wrong one.
    /// </summary>
    public static DateOnly? DateOf(Act act)
    {
        ArgumentNullException.ThrowIfNull(act);

        return act.Direction == ActDirection.Outgoing
            ? act.Sent ?? act.Drafted ?? act.Delivered ?? act.Received
            : act.Received ?? act.Delivered ?? act.Sent ?? act.Drafted;
    }

    /// <summary>
    /// One sequence, oldest first. Entries with no date sort last — a document with no date is still in
    /// the file, and dropping it would make the timeline quietly incomplete.
    /// </summary>
    /// <param name="acts">Acts of the whole tree, each with its case loaded.</param>
    /// <param name="comments">Comments of the whole tree. A comment on an act is placed under the act's case.</param>
    public static IReadOnlyList<TimelineEntry> Merge(IEnumerable<Act> acts, IEnumerable<Comment> comments)
    {
        ArgumentNullException.ThrowIfNull(acts);
        ArgumentNullException.ThrowIfNull(comments);

        var entries = acts.Select(EntryFor).Concat(comments.Select(EntryFor));

        return
        [
            .. entries
                .OrderBy(entry => entry.OccurredOn is null)
                .ThenBy(entry => entry.OccurredOn)
                .ThenBy(entry => entry.Kind)
                .ThenBy(entry => entry.SourceId),
        ];
    }

    private static TimelineEntry EntryFor(Act act) => new()
    {
        Kind = TimelineEntryKind.Act,
        SourceId = act.Id,
        OccurredOn = DateOf(act),
        Title = act.Title,
        CaseId = act.CaseId,
        CaseTitle = act.Case?.Title ?? "",
        Direction = act.Direction,
    };

    private static TimelineEntry EntryFor(Comment comment) => new()
    {
        Kind = TimelineEntryKind.Comment,
        SourceId = comment.Id,
        OccurredOn = DateOnly.FromDateTime(comment.Created),
        Title = comment.Body,

        // A note on an act belongs to that act's case, which is what makes both kinds comparable.
        CaseId = comment.CaseId ?? comment.Act?.CaseId ?? 0,
        CaseTitle = comment.Case?.Title ?? comment.Act?.Case?.Title ?? "",
    };
}
