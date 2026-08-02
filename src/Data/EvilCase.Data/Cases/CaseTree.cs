using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Data.Cases;

/// <summary>
/// Walks a loaded case graph. Pure over the navigation properties, so it says nothing about how the
/// graph was loaded — a merged timeline over a whole sub-tree (M4) will fetch it in one query instead.
/// </summary>
/// <remarks>
/// Every walk carries a visited set. <see cref="CanNestUnder"/> is what keeps a cycle out of the data in
/// the first place, but a walk that hangs on a graph that got one anyway is a far worse failure than a
/// walk that stops.
/// </remarks>
public static class CaseTree
{
    /// <summary>
    /// Every case under <paramref name="root"/>, breadth-first, nearest generation first. Excludes the
    /// root itself.
    /// </summary>
    public static IReadOnlyList<Case> Descendants(Case root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var visited = NewVisitedSet();
        _ = visited.Add(root);

        var descendants = new List<Case>();
        var generation = new Queue<Case>(root.Children);

        while (generation.TryDequeue(out var current))
        {
            if (!visited.Add(current))
                continue;

            descendants.Add(current);

            foreach (var child in current.Children)
                generation.Enqueue(child);
        }

        return descendants;
    }

    /// <summary>
    /// The chain from the parent of <paramref name="case"/> up to its root, nearest first. Empty on a
    /// root case.
    /// </summary>
    public static IReadOnlyList<Case> Ancestors(Case @case)
    {
        ArgumentNullException.ThrowIfNull(@case);

        var visited = NewVisitedSet();
        _ = visited.Add(@case);

        var ancestors = new List<Case>();

        for (var parent = @case.Parent; parent is not null && visited.Add(parent); parent = parent.Parent)
            ancestors.Add(parent);

        return ancestors;
    }

    /// <summary>
    /// Zero on a root case, one on its sub-case, and so on.
    /// </summary>
    public static int Depth(Case @case) => Ancestors(@case).Count;

    /// <summary>
    /// Whether <paramref name="case"/> may hang under <paramref name="parent"/>. A null parent makes it
    /// a root and is always allowed; itself or one of its own descendants would close a cycle and is
    /// refused, which is the only thing that can turn the tree into a graph.
    /// </summary>
    public static bool CanNestUnder(Case @case, Case? parent)
    {
        ArgumentNullException.ThrowIfNull(@case);

        if (parent is null)
            return true;

        if (ReferenceEquals(@case, parent))
            return false;

        return !Descendants(@case).Any(descendant => ReferenceEquals(descendant, parent));
    }

    // Reference equality throughout, hence the object element type: a Case is a record, so structural
    // equality would call two distinct rows the same while they are unsaved and share an Id of zero.
    private static HashSet<object> NewVisitedSet() => new(ReferenceEqualityComparer.Instance);
}
