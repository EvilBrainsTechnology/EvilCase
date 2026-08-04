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
    public void TheSubTreeNestsToAnyDepth()
    {
        var subCases = CaseGraph.SubCases(Line, 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(subCases.Select(node => node.Id), Is.EqualTo(new long[] { 2 }));
            Assert.That(subCases[0].Children.Select(node => node.Id), Is.EqualTo(new long[] { 3, 5 }), "siblings order by case number");
            Assert.That(subCases[0].Children[0].Children.Select(node => node.Id), Is.EqualTo(new long[] { 4 }));
        }
    }

    [Test]
    public void ALeafCarriesNoChildrenAndNoOtherRootLeaksIn()
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
    /// Nothing can create a cycle today; the walk survives one anyway rather than running forever.
    /// </summary>
    [Test]
    public void ACycleEndsTheWalkInsteadOfRunningForever()
    {
        CaseGraphNode[] cycle =
        [
            Node(1, parent: 2, "EC-001"),
            Node(2, parent: 1, "EC-002"),
        ];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseGraph.Ancestors(cycle, 1).Select(ancestor => ancestor.Id), Is.EqualTo(new long[] { 2 }));
            Assert.That(CaseGraph.SubCases(cycle, 1)[0].Children, Is.Empty);
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
