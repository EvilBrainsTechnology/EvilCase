using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The flat list the API sends becomes the tree the screen renders here, so nothing else catches a
/// sub-case that goes missing on the way.
/// </summary>
public class CaseTreeBranchTests
{
    private static readonly CaseTreeNode Case = Node(1, parent: 0, "EC-001");

    /// <summary>
    /// 1 → 2 → 3 → 4, with 5 a second child of 2.
    /// </summary>
    private static readonly CaseTreeNode[] Line =
    [
        Node(2, parent: 1, "EC-002"),
        Node(3, parent: 2, "EC-003"),
        Node(4, parent: 3, "EC-004"),
        Node(5, parent: 2, "EC-005"),
    ];

    [Test]
    public void TheTreeNestsAsDeepAsTheListGoes()
    {
        var tree = CaseTreeBranch.Nest(Case, Line);

        Assert.That(Paths(tree), Is.EqualTo(["1", "1/2", "1/2/3", "1/2/3/4", "1/2/5"]), "a node hangs under the case it names");
    }

    [Test]
    public void TheCaseItselfHeadsTheTree()
    {
        var tree = CaseTreeBranch.Nest(Case, Line);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tree.Node, Is.SameAs(Case), "the tree reads from the case down");
            Assert.That(tree.Children.Select(branch => branch.Node.Id), Is.EqualTo(new long[] { 2 }), "only what hangs under it directly is a child");
        }
    }

    [Test]
    public void ACaseWithNoSubCasesIsATreeOfOne()
    {
        var tree = CaseTreeBranch.Nest(Case, []);

        Assert.That(Paths(tree), Is.EqualTo(["1"]), "an empty list still reads as the case itself");
    }

    /// <summary>
    /// The API sends siblings by case number; the tree keeps whatever order it is handed rather than
    /// deciding one of its own.
    /// </summary>
    [Test]
    public void SiblingsKeepTheOrderTheListSendsThemIn()
    {
        CaseTreeNode[] siblings =
        [
            Node(4, parent: 1, "EC-100"),
            Node(2, parent: 1, "EC-200"),
            Node(3, parent: 1, "EC-300"),
        ];

        var tree = CaseTreeBranch.Nest(Case, siblings);

        Assert.That(Paths(tree), Is.EqualTo(["1", "1/4", "1/2", "1/3"]), "the order the list arrives in is the order the tree shows");
    }

    /// <summary>
    /// Parents-first is what the API promises, not what the tree may rely on.
    /// </summary>
    [Test]
    public void ANodeArrivingBeforeTheOneItHangsUnderStillNests()
    {
        CaseTreeNode[] reversed =
        [
            Node(3, parent: 2, "EC-003"),
            Node(2, parent: 1, "EC-002"),
        ];

        var tree = CaseTreeBranch.Nest(Case, reversed);

        Assert.That(Paths(tree), Is.EqualTo(["1", "1/2", "1/2/3"]), "the order the rows arrive in does not decide the shape");
    }

    [Test]
    public void ANodeWhoseParentNeverArrivedKeepsItsOwnSubTree()
    {
        CaseTreeNode[] gapped =
        [
            Node(3, parent: 2, "EC-003"),
            Node(4, parent: 3, "EC-004"),
            Node(5, parent: 1, "EC-005"),
        ];

        var tree = CaseTreeBranch.Nest(Case, gapped);

        Assert.That(Paths(tree), Is.EqualTo(["1", "1/5", "1/3", "1/3/4"]), "a node the list never places hangs under the case itself, sub-tree and all");
    }

    [Test]
    public void EveryCaseTheListNamesIsShownExactlyOnce()
    {
        CaseTreeNode[] tangled =
        [
            Node(3, parent: 2, "EC-003"),
            Node(7, parent: 6, "EC-007"),
            Node(6, parent: 7, "EC-006"),
            Node(2, parent: 1, "EC-002"),
        ];

        var tree = CaseTreeBranch.Nest(Case, tangled);

        Assert.That(Paths(tree), Is.EqualTo(["1", "1/2", "1/2/3", "1/7", "1/7/6"]), "the tree the screen shows is never smaller than the case is");
    }

    /// <summary>
    /// Only the walk's visited set keeps this off today's server, so the tree does not lean on it.
    /// </summary>
    [Test]
    public void ADuplicateIdentifierDoesNotReParentACase()
    {
        CaseTreeNode[] duplicated =
        [
            Node(2, parent: 1, "EC-002"),
            Node(1, parent: 2, "EC-DUP"),
            Node(9, parent: 1, "EC-009"),
        ];

        var tree = CaseTreeBranch.Nest(Case, duplicated);

        Assert.That(Paths(tree), Is.EqualTo(["1", "1/2", "1/9"]), "a case already in the tree keeps the place it has");
    }

    [Test]
    public void ACaseThatHangsUnderItselfIsStillReadOnce()
    {
        CaseTreeNode[] looped =
        [
            Node(2, parent: 1, "EC-002"),
            Node(3, parent: 3, "EC-003"),
        ];

        var tree = CaseTreeBranch.Nest(Case, looped);

        Assert.That(Paths(tree), Is.EqualTo(["1", "1/2", "1/3"]), "a self-reference does not nest a case under itself forever");
    }

    /// <summary>
    /// Every node depth-first, each as the identifiers from the case down to it.
    /// </summary>
    private static List<string> Paths(CaseTreeBranch tree)
    {
        var paths = new List<string>();

        Walk(tree, above: "", paths);

        return paths;
    }

    private static void Walk(CaseTreeBranch branch, string above, List<string> paths)
    {
        var path = above + branch.Node.Id.ToString(CultureInfo.InvariantCulture);

        paths.Add(path);

        foreach (var child in branch.Children)
            Walk(child, path + "/", paths);
    }

    private static CaseTreeNode Node(long id, long parent, string caseNumber) => new()
    {
        Id = id,
        ParentId = parent,
        CaseNumber = caseNumber,
        Title = $"Case {id.ToString(CultureInfo.InvariantCulture)}",
        Status = CaseStatus.Active,
    };
}
