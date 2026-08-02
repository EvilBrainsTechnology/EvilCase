using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Cases;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The sub-case tree is the shape the whole product hangs off — a merged timeline, an import and every
/// owner-scoped query all read it — so the walks over it are pinned before anything depends on them.
/// </summary>
public class CaseTreeTests
{
    private static readonly DateTime Created = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly string[] EveryGeneration = ["first", "second", "grandchild", "great-grandchild"];

    private static readonly string[] TheChildAlone = ["child"];

    [Test]
    public void DescendantsCoverEveryGenerationNearestFirst()
    {
        var root = NewCase("root");
        var first = NewCase("first", root);
        var second = NewCase("second", root);
        var grandchild = NewCase("grandchild", first);
        var greatGrandchild = NewCase("great-grandchild", grandchild);

        var descendants = CaseTree.Descendants(root);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                descendants.Select(descendant => descendant.Title),
                Is.EqualTo(EveryGeneration),
                "breadth-first, so a generation is complete before the next one starts");

            Assert.That(descendants, Does.Not.Contain(root), "the root is not its own descendant");
            Assert.That(descendants, Has.Member(greatGrandchild), "depth is not capped");
            Assert.That(CaseTree.Descendants(second), Is.Empty, "a leaf has none");
        }
    }

    [Test]
    public void AncestorsRunFromTheParentUpToTheRoot()
    {
        var root = NewCase("root");
        var child = NewCase("child", root);
        var grandchild = NewCase("grandchild", child);

        Case[] nearestFirst = [child, root];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseTree.Ancestors(grandchild), Is.EqualTo(nearestFirst));
            Assert.That(CaseTree.Ancestors(root), Is.Empty, "a root case has none");
        }
    }

    [Test]
    public void DepthCountsTheGenerationsAboveACase()
    {
        var root = NewCase("root");
        var child = NewCase("child", root);
        var grandchild = NewCase("grandchild", child);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseTree.Depth(root), Is.Zero);
            Assert.That(CaseTree.Depth(child), Is.EqualTo(1));
            Assert.That(CaseTree.Depth(grandchild), Is.EqualTo(2));
        }
    }

    [Test]
    public void ACaseCannotNestUnderItselfOrItsOwnDescendants()
    {
        var root = NewCase("root");
        var child = NewCase("child", root);
        var grandchild = NewCase("grandchild", child);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseTree.CanNestUnder(root, root), Is.False, "a case is not its own parent");
            Assert.That(CaseTree.CanNestUnder(root, child), Is.False, "the parent would hang under its child");
            Assert.That(CaseTree.CanNestUnder(root, grandchild), Is.False, "any depth of descendant closes the cycle");
        }
    }

    [Test]
    public void ACaseNestsUnderAnythingOutsideItsOwnSubTree()
    {
        var root = NewCase("root");
        var child = NewCase("child", root);
        var elsewhere = NewCase("elsewhere");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseTree.CanNestUnder(child, elsewhere), Is.True, "an unrelated case is a valid parent");
            Assert.That(CaseTree.CanNestUnder(child, parent: null), Is.True, "and a sub-case may be promoted to a root");
            Assert.That(CaseTree.CanNestUnder(elsewhere, child), Is.True, "the ancestor side is what matters, not the descendant one");
        }
    }

    /// <summary>
    /// <see cref="CaseTree.CanNestUnder"/> is what keeps this out of the data. If it ever gets in
    /// anyway, a walk has to stop rather than hang.
    /// </summary>
    [Test]
    public void AWalkOverAGraphThatAlreadyHasACycleTerminates()
    {
        var root = NewCase("root");
        var child = NewCase("child", root);

        child.Children.Add(root);

        var descendants = CaseTree.Descendants(root);

        Assert.That(
            descendants.Select(descendant => descendant.Title),
            Is.EqualTo(TheChildAlone),
            "the root is already visited, so the walk closes there");
    }

    private static Case NewCase(string title, Case? parent = null)
    {
        var @case = new Case
        {
            OwnerId = 1,
            InternalReference = $"EC20260801-{title.GetHashCode(StringComparison.Ordinal):X8}",
            Title = title,
            Status = CaseStatus.Active,
            Created = Created,
            Parent = parent,
            ParentCaseId = parent?.Id,
        };

        parent?.Children.Add(@case);

        return @case;
    }
}
