using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Turns the flat rows of a tree walk into what the detail shows. Pure over
/// <see cref="CaseGraphNode.ParentCaseId"/> and carrying a visited set, so a chain that closes a cycle
/// ends instead of running forever.
/// </summary>
public static class CaseGraph
{
    /// <summary>
    /// The chain from the root down to the parent of <paramref name="id"/>. Empty on a root case.
    /// </summary>
    public static IReadOnlyList<CaseAncestor> Ancestors(IReadOnlyList<CaseGraphNode> nodes, long id)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var byId = nodes.ToDictionary(node => node.Id);
        HashSet<long> visited = [id];
        var ancestors = new List<CaseAncestor>();

        var parentId = byId.TryGetValue(id, out var start) ? start.ParentCaseId : null;

        while (parentId is { } current && visited.Add(current) && byId.TryGetValue(current, out var parent))
        {
            ancestors.Add(new CaseAncestor { Id = parent.Id, CaseNumber = parent.CaseNumber, Title = parent.Title });
            parentId = parent.ParentCaseId;
        }

        ancestors.Reverse();

        return ancestors;
    }

    /// <summary>
    /// The sub-cases of <paramref name="id"/> nested to any depth, siblings ordered by case number.
    /// </summary>
    public static IReadOnlyList<CaseTreeNode> SubCases(IReadOnlyList<CaseGraphNode> nodes, long id)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var byParent = nodes
            .Where(node => node.ParentCaseId is not null)
            .GroupBy(node => node.ParentCaseId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(node => node.CaseNumber, StringComparer.Ordinal).ToList());

        HashSet<long> visited = [id];

        return Children(byParent, id, visited);
    }

    private static List<CaseTreeNode> Children(
        Dictionary<long, List<CaseGraphNode>> byParent,
        long parentId,
        HashSet<long> visited)
    {
        if (!byParent.TryGetValue(parentId, out var children))
            return [];

        var nodes = new List<CaseTreeNode>(children.Count);

        foreach (var child in children)
        {
            if (!visited.Add(child.Id))
                continue;

            nodes.Add(new CaseTreeNode
            {
                Id = child.Id,
                CaseNumber = child.CaseNumber,
                Title = child.Title,
                Status = child.Status,
                Children = Children(byParent, child.Id, visited),
            });
        }

        return nodes;
    }
}
