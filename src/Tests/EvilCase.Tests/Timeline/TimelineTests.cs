using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Timeline;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Timeline;

namespace EvilBrains.EvilCase.Tests.Timeline;

/// <summary>
/// Priority #1 in <c>docs/product/vision.md</c>: one timeline of the whole case including all
/// sub-cases. What is merged is passed in, so the ordering is pinned without a database.
/// </summary>
public class TimelineTests
{
    private static readonly DateTime Midnight = new(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc);

    [Test]
    public void AnOutgoingActHappensWhenItWasSentAndAnIncomingOneWhenItArrived()
    {
        var filed = NewAct(1, ActDirection.Outgoing) with
        {
            Drafted = new(2026, 3, 1),
            Sent = new(2026, 3, 4),
            Delivered = new(2026, 3, 6),
        };

        var arrived = NewAct(2, ActDirection.Incoming) with
        {
            Sent = new(2026, 3, 10),
            Received = new(2026, 3, 12),
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseTimeline.DateOf(filed), Is.EqualTo(new DateOnly(2026, 3, 4)), "sent, not drafted and not delivered");
            Assert.That(CaseTimeline.DateOf(arrived), Is.EqualTo(new DateOnly(2026, 3, 12)), "received, not when the authority sent it");
        }
    }

    [Test]
    public void AnActFallsBackThroughTheDatesThatAreNearlyTheSameThing()
    {
        var draftedOnly = NewAct(1, ActDirection.Outgoing) with { Drafted = new(2026, 3, 1) };
        var deliveredOnly = NewAct(2, ActDirection.Incoming) with { Delivered = new(2026, 3, 2) };
        var undated = NewAct(3, ActDirection.Outgoing);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseTimeline.DateOf(draftedOnly), Is.EqualTo(new DateOnly(2026, 3, 1)));
            Assert.That(CaseTimeline.DateOf(deliveredOnly), Is.EqualTo(new DateOnly(2026, 3, 2)));
            Assert.That(CaseTimeline.DateOf(undated), Is.Null, "no date at all beats a wrong one");
        }
    }

    [Test]
    public void ActsAndCommentsFromEveryCaseComeBackAsOneSequenceOldestFirst()
    {
        var root = NewCase(10, "root");
        var sub = NewCase(11, "sub-case");

        Act[] acts =
        [
            NewAct(1, ActDirection.Outgoing, root) with { Sent = new(2026, 3, 4), Title = "žaloba" },
            NewAct(2, ActDirection.Incoming, sub) with { Received = new(2026, 3, 1), Title = "rozhodnutí" },
        ];

        Comment[] comments = [NewComment(5, root, Midnight.AddDays(1), "a note")];

        var merged = CaseTimeline.Merge(acts, comments);
        string[] oldestFirst = ["rozhodnutí", "žaloba", "a note"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(merged.Select(entry => entry.Title), Is.EqualTo(oldestFirst));
            Assert.That(merged[0].CaseId, Is.EqualTo(11), "an entry says which case it came from");
            Assert.That(merged[0].CaseTitle, Is.EqualTo("sub-case"), "which is the whole point of merging");
            Assert.That(merged[0].Kind, Is.EqualTo(TimelineEntryKind.Act));
            Assert.That(merged[2].Kind, Is.EqualTo(TimelineEntryKind.Comment));
            Assert.That(merged[0].Direction, Is.EqualTo(ActDirection.Incoming));
            Assert.That(merged[2].Direction, Is.Null, "a comment has no direction");
        }
    }

    [Test]
    public void ANoteOnAnActBelongsToThatActsCase()
    {
        var sub = NewCase(11, "sub-case");
        var act = NewAct(1, ActDirection.Incoming, sub);

        var onTheAct = new Comment
        {
            Id = 7,
            ActId = act.Id,
            Act = act,
            Body = "note on the act",
            AuthorUserId = 1,
            Created = Midnight,
        };

        var merged = CaseTimeline.Merge([], [onTheAct]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(merged[0].CaseId, Is.EqualTo(11), "reached through the act, so both kinds are comparable");
            Assert.That(merged[0].CaseTitle, Is.EqualTo("sub-case"));
        }
    }

    [Test]
    public void AnUndatedEntrySortsLastRatherThanVanishing()
    {
        Act[] acts =
        [
            NewAct(1, ActDirection.Outgoing) with { Title = "undated" },
            NewAct(2, ActDirection.Outgoing) with { Sent = new(2026, 3, 4), Title = "dated" },
        ];

        var merged = CaseTimeline.Merge(acts, []);
        string[] datedFirst = ["dated", "undated"];

        Assert.That(
            merged.Select(entry => entry.Title),
            Is.EqualTo(datedFirst),
            "a document with no date is still in the file");
    }

    private static Case NewCase(long id, string title) => new()
    {
        Id = id,
        OwnerId = 1,

        // Required since the mark moved onto the case itself; the timeline never reads it.
        InternalCaseReference = $"EC-{id.ToString(CultureInfo.InvariantCulture)}",
        Title = title,
        Status = CaseStatus.Active,
        Created = Midnight,
    };

    private static Act NewAct(long id, ActDirection direction, Case? @case = null)
    {
        var owner = @case ?? NewCase(10, "root");

        return new()
        {
            Id = id,
            CaseId = owner.Id,
            Case = owner,
            Ordinal = (int)id,
            Direction = direction,
            Title = string.Create(CultureInfo.InvariantCulture, $"act {id}"),
            Created = Midnight,
        };
    }

    private static Comment NewComment(long id, Case @case, in DateTime created, string body) => new()
    {
        Id = id,
        CaseId = @case.Id,
        Case = @case,
        Body = body,
        AuthorUserId = 1,
        Created = created,
    };
}
