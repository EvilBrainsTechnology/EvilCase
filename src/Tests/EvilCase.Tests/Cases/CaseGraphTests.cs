using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// Nesting is the one shape the database cannot return, so it is a pure function over the flat rows.
/// </summary>
public class CaseGraphTests
{
    /// <summary>
    /// 1 → 2 → 3 → 4, with 5 a second child of 2 and 9 a case of its own.
    /// </summary>
    private static readonly CaseGraphNode[] Line =
    [
        Node(1, parent: null, "EC-001"),
        Node(2, parent: 1, "EC-002"),
        Node(3, parent: 2, "EC-003"),
        Node(4, parent: 3, "EC-004"),
        Node(5, parent: 2, "EC-005"),
        Node(9, parent: null, "EC-009"),
    ];

    [Test]
    public void ARootCaseHasNoAncestors() =>
        Assert.That(CaseGraph.Ancestors(Line, 1), Is.Empty);

    [Test]
    public void AncestorsRunFromTheRootDownToTheParent()
    {
        var ancestors = CaseGraph.Ancestors(Line, 4).Select(ancestor => ancestor.Id);

        Assert.That(ancestors, Is.EqualTo(new long[] { 1, 2, 3 }), "the path reads root first");
    }

    [Test]
    public void TheSubTreeReadsToAnyDepthEachNodeAfterTheOneItHangsUnder()
    {
        var subCases = CaseGraph.SubCases(Line, 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(subCases.Select(node => node.Id), Is.EqualTo(new long[] { 2, 3, 4, 5 }), "a node follows the one it hangs under");
            Assert.That(subCases.Select(node => node.ParentId), Is.EqualTo(new long[] { 1, 2, 3, 2 }), "a node carries the case it hangs under");
        }
    }

    /// <summary>
    /// The identifiers run against the case numbers, so a walk that kept the order it was handed fails.
    /// The query has no ORDER BY: rows arrive in whatever order the server gives.
    /// </summary>
    [Test]
    public void SiblingsReadInCaseNumberOrderWhateverOrderTheRowsArriveIn()
    {
        CaseGraphNode[] unordered =
        [
            Node(1, parent: null, "EC-001"),
            Node(2, parent: 1, "EC-900"),
            Node(3, parent: 1, "EC-100"),
            Node(4, parent: 1, "EC-500"),
        ];

        var subCases = CaseGraph.SubCases(unordered, 1);

        Assert.That(subCases.Select(node => node.CaseNumber), Is.EqualTo(["EC-100", "EC-500", "EC-900"]), "siblings order by case number");
    }

    [Test]
    public void ALeafCarriesNoSubCasesAndNoOtherRootLeaksIn()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseGraph.SubCases(Line, 4), Is.Empty);
            Assert.That(CaseGraph.SubCases(Line, 1).Select(node => node.Id), Does.Not.Contain(9L));
        }
    }

    [Test]
    public void TheWalkedNodeKeepsWhatTheTreeShows()
    {
        var node = CaseGraph.SubCases(Line, 3)[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.CaseNumber, Is.EqualTo("EC-004"));
            Assert.That(node.Title, Is.EqualTo("Case 4"));
            Assert.That(node.Status, Is.EqualTo(CaseStatus.Active));
        }
    }

    /// <summary>
    /// What the CTE returns for a cycle read from 2, where 1 hangs under 2, 2 under 3 and 3 under 1: the
    /// walk goes up and down separately, so every case of the cycle arrives from both directions.
    /// <see cref="CaseWalkDatabaseTests"/> reads these rows off a server.
    /// </summary>
    [Test]
    public void ACaseArrivingFromBothDirectionsIsReadOnce()
    {
        CaseGraphNode[] cycle =
        [
            Node(2, parent: 3, "EC-002"),
            Node(3, parent: 1, "EC-003"),
            Node(1, parent: 2, "EC-001"),
            Node(1, parent: 2, "EC-001"),
            Node(3, parent: 1, "EC-003"),
        ];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseGraph.Ancestors(cycle, 2).Select(ancestor => ancestor.Id), Is.EqualTo(new long[] { 1, 3 }), "a repeated row is read once");
            Assert.That(CaseGraph.SubCases(cycle, 2).Select(node => node.Id), Is.EqualTo(new long[] { 1, 3 }), "the walk ends on a case it has already read");
        }
    }

    private static CaseGraphNode Node(long id, long? parent, string caseNumber) => new()
    {
        Id = id,
        ParentCaseId = parent,
        CaseNumber = caseNumber,
        Title = $"Case {id.ToString(CultureInfo.InvariantCulture)}",
        Status = CaseStatus.Active,
    };
}
