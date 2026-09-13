namespace EvilBrains.EvilCase.Domain.Cases;

public static class CaseHierarchy
{
    /// <summary>
    /// parents must hold every case of the tenant, or a loop is missed.
    /// </summary>
    public static bool WouldFormCycle(IReadOnlyDictionary<Guid, Guid?> parents, Guid caseId, Guid parentCaseId)
    {
        var ancestor = (Guid?)parentCaseId;

        // The walk never outlives the map: a loop already in the data would otherwise never end.
        for (var step = 0; ancestor is not null && step <= parents.Count; step++)
        {
            if (ancestor == caseId)
                return true;

            ancestor = parents.TryGetValue(ancestor.Value, out var next) ? next : null;
        }

        return false;
    }

    /// <summary>
    /// parents must hold every case of the tenant, or a subordinate case is missed.
    /// </summary>
    public static IReadOnlySet<Guid> WithSubordinates(IReadOnlyDictionary<Guid, Guid?> parents, Guid caseId)
    {
        var children = parents
            .Where(static link => link.Value is not null)
            .GroupBy(static link => link.Value!.Value, static link => link.Key)
            .ToDictionary(static group => group.Key, static group => group.ToList());

        var subtree = new HashSet<Guid> { caseId };
        var pending = new Stack<Guid>([caseId]);

        // Each id is visited once: a loop already in the data would otherwise never end.
        while (pending.TryPop(out var current))
        {
            if (!children.TryGetValue(current, out var currentChildren))
                continue;

            foreach (var child in currentChildren)
            {
                if (subtree.Add(child))
                    pending.Push(child);
            }
        }

        return subtree;
    }
}
