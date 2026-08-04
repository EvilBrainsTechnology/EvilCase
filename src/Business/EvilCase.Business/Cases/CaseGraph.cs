using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Turns the flat rows of a tree walk into what the detail shows. Pure over
/// <see cref="CaseGraphNode.ParentCaseId"/>: a case reached both ways arrives twice, so both walks read one
/// row per identifier and carry a visited set.
/// </summary>
public static class CaseGraph
{
    /// <summary>
    /// The chain from the root down to the parent of <paramref name="id"/>. Empty on a root case.
    /// </summary>
    public static IReadOnlyList<CaseAncestor> Ancestors(IReadOnlyList<CaseGraphNode> nodes, long id)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var byId = ById(nodes);
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
    /// The sub-cases of <paramref name="id"/> to any depth, flat and each after the one it hangs under,
    /// siblings by case number. The walk is iterative, so depth costs no stack.
    /// </summary>
    public static IReadOnlyList<CaseTreeNode> SubCases(IReadOnlyList<CaseGraphNode> nodes, long id)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var byParent = ById(nodes).Values
            .Where(node => node.ParentCaseId is not null)
            .GroupBy(node => node.ParentCaseId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(node => node.CaseNumber, StringComparer.Ordinal).ToList());

        HashSet<long> visited = [id];
        var subCases = new List<CaseTreeNode>();
        var pending = new Stack<CaseGraphNode>();

        Push(pending, byParent, id);

        while (pending.TryPop(out var node))
        {
            if (!visited.Add(node.Id))
                continue;

            subCases.Add(new CaseTreeNode
            {
                Id = node.Id,
                ParentId = node.ParentCaseId!.Value,
                CaseNumber = node.CaseNumber,
                Title = node.Title,
                Status = node.Status,
            });

            Push(pending, byParent, node.Id);
        }

        return subCases;
    }

    /// <summary>
    /// Reversed, so the stack hands the children back in case-number order.
    /// </summary>
    private static void Push(Stack<CaseGraphNode> pending, Dictionary<long, List<CaseGraphNode>> byParent, long parentId)
    {
        if (!byParent.TryGetValue(parentId, out var children))
            return;

        for (var index = children.Count - 1; index >= 0; index--)
            pending.Push(children[index]);
    }

    private static Dictionary<long, CaseGraphNode> ById(IReadOnlyList<CaseGraphNode> nodes)
    {
        var byId = new Dictionary<long, CaseGraphNode>(nodes.Count);

        foreach (var node in nodes)
            byId[node.Id] = node;

        return byId;
    }
}
