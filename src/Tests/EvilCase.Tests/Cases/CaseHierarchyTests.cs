using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseHierarchyTests
{
    [Test]
    public void ACaseIsNeverItsOwnParent()
    {
        var a = Guid.CreateVersion7();
        var map = new Dictionary<Guid, Guid?> { [a] = null };

        Assert.That(CaseHierarchy.WouldFormCycle(map, a, a), Is.True);
    }

    [Test]
    public void ACaseNeverHangsUnderItsOwnChild()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var map = new Dictionary<Guid, Guid?> { [a] = null, [b] = a };

        Assert.That(CaseHierarchy.WouldFormCycle(map, a, b), Is.True);
    }

    [Test]
    public void ACaseNeverHangsUnderItsOwnGrandchild()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var c = Guid.CreateVersion7();
        var map = new Dictionary<Guid, Guid?> { [a] = null, [b] = a, [c] = b };

        Assert.That(CaseHierarchy.WouldFormCycle(map, a, c), Is.True);
    }

    [Test]
    public void AnUnrelatedCaseIsAParent()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var map = new Dictionary<Guid, Guid?> { [a] = null, [b] = null };

        Assert.That(CaseHierarchy.WouldFormCycle(map, a, b), Is.False);
    }

    [Test]
    public void ASiblingIsAParent()
    {
        var root = Guid.CreateVersion7();
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var map = new Dictionary<Guid, Guid?> { [root] = null, [a] = root, [b] = root };

        Assert.That(CaseHierarchy.WouldFormCycle(map, a, b), Is.False);
    }

    [Test]
    public void ALoopAlreadyInTheDataDoesNotHangTheWalk()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var c = Guid.CreateVersion7();
        var map = new Dictionary<Guid, Guid?> { [a] = b, [b] = a, [c] = null };

        Assert.That(
            CaseHierarchy.WouldFormCycle(map, c, a),
            Is.False,
            "the walk is capped by the map's size, so data that already loops still answers");
    }

    [Test]
    public void ACaseWithoutSubordinatesStandsAlone()
    {
        var a = Guid.CreateVersion7();
        var map = new Dictionary<Guid, Guid?> { [a] = null };

        Assert.That(CaseHierarchy.WithSubordinates(map, a), Is.EquivalentTo([a]));
    }

    [Test]
    public void ASubordinateCaseBelongsToTheSubtree()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var map = new Dictionary<Guid, Guid?> { [a] = null, [b] = a };

        Assert.That(CaseHierarchy.WithSubordinates(map, a), Is.EquivalentTo([a, b]));
    }

    [Test]
    public void TheSubtreeReachesEveryLevelBelowTheCase()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var c = Guid.CreateVersion7();
        var d = Guid.CreateVersion7();
        var map = new Dictionary<Guid, Guid?> { [a] = null, [b] = a, [c] = b, [d] = c };

        Assert.That(CaseHierarchy.WithSubordinates(map, a), Is.EquivalentTo([a, b, c, d]));
    }

    [Test]
    public void TheSubtreeHoldsNoSiblingAndNoUnrelatedCase()
    {
        var root = Guid.CreateVersion7();
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var child = Guid.CreateVersion7();
        var unrelated = Guid.CreateVersion7();
        var map = new Dictionary<Guid, Guid?> { [root] = null, [a] = root, [b] = root, [child] = a, [unrelated] = null };

        Assert.That(CaseHierarchy.WithSubordinates(map, a), Is.EquivalentTo([a, child]));
    }

    [Test]
    public void ALoopAlreadyInTheDataDoesNotHangTheSubtreeWalk()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var map = new Dictionary<Guid, Guid?> { [a] = b, [b] = a };

        Assert.That(
            CaseHierarchy.WithSubordinates(map, a),
            Is.EquivalentTo([a, b]),
            "each case is visited once, so data that already loops still answers");
    }
}
