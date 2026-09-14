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
}
