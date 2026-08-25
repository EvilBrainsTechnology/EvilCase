namespace EvilBrains.EvilCase.Domain.Cases;

/// <summary>
/// The rule that keeps the case hierarchy loop-free: a case is never its own ancestor (SDD-009).
/// </summary>
public static class CaseHierarchy
{
    /// <summary>
    /// Whether hanging <paramref name="caseId"/> under <paramref name="parentCaseId"/> would close a loop.
    /// <paramref name="parents"/> maps every case of the tenant to its parent.
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
}
