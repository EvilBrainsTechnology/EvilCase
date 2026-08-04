using EvilBrains.EvilCase.Api.Contract.Timeline;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Business.Timeline;

/// <summary>
/// Turns the acts and comments of a case tree into one chronological sequence.
/// </summary>
public static class CaseTimeline
{
    /// <summary>
    /// When an act happened: an outgoing one when sent, an incoming one when it arrived, each falling
    /// back through the near-equivalent dates.
    /// </summary>
    public static DateOnly? DateOf(Act act)
    {
        ArgumentNullException.ThrowIfNull(act);

        return act.Direction == ActDirection.Outgoing
            ? act.Sent ?? act.Drafted ?? act.Delivered ?? act.Received
            : act.Received ?? act.Delivered ?? act.Sent ?? act.Drafted;
    }

    /// <summary>
    /// One sequence, oldest first, entries with no date last.
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

        // A note on an act belongs to that act's case.
        CaseId = comment.CaseId ?? comment.Act?.CaseId ?? 0,
        CaseTitle = comment.Case?.Title ?? comment.Act?.Case?.Title ?? "",
    };
}
