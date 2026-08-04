using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// A case with the sub-cases hanging under it, rebuilt from the flat list the API sends.
/// </summary>
public sealed record CaseTreeBranch
{
    public required CaseTreeNode Node { get; init; }

    public required IReadOnlyList<CaseTreeBranch> Children { get; init; }

    /// <summary>
    /// Every node the list names, once, however it is ordered: a node the list never places hangs under
    /// the case itself with its own sub-tree, and a repeated identifier keeps the place it already has.
    /// The screen never shows a smaller tree than the case has. The walk is iterative, so depth costs no
    /// stack.
    /// </summary>
    public static CaseTreeBranch Nest(CaseTreeNode root, IReadOnlyList<CaseTreeNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(nodes);

        var childrenOf = ChildrenOf(nodes);
        var placed = new Dictionary<long, List<CaseTreeBranch>>();

        var tree = Place(root, placed, under: null);
        Grow(root.Id, childrenOf, placed);

        foreach (var node in nodes)
        {
            if (placed.ContainsKey(node.Id))
                continue;

            _ = Place(node, placed, placed[root.Id]);
            Grow(node.Id, childrenOf, placed);
        }

        return tree;
    }

    private static Dictionary<long, List<CaseTreeNode>> ChildrenOf(IReadOnlyList<CaseTreeNode> nodes)
    {
        var childrenOf = new Dictionary<long, List<CaseTreeNode>>();

        foreach (var node in nodes)
        {
            if (!childrenOf.TryGetValue(node.ParentId, out var siblings))
            {
                siblings = [];
                childrenOf[node.ParentId] = siblings;
            }

            siblings.Add(node);
        }

        return childrenOf;
    }

    private static CaseTreeBranch Place(CaseTreeNode node, Dictionary<long, List<CaseTreeBranch>> placed, List<CaseTreeBranch>? under)
    {
        var children = new List<CaseTreeBranch>();
        var branch = new CaseTreeBranch { Node = node, Children = children };

        placed[node.Id] = children;
        under?.Add(branch);

        return branch;
    }

    private static void Grow(long id, Dictionary<long, List<CaseTreeNode>> childrenOf, Dictionary<long, List<CaseTreeBranch>> placed)
    {
        var pending = new Stack<long>();

        pending.Push(id);

        while (pending.TryPop(out var parent))
        {
            if (!childrenOf.TryGetValue(parent, out var children))
                continue;

            foreach (var child in children)
            {
                if (placed.ContainsKey(child.Id))
                    continue;

                _ = Place(child, placed, placed[parent]);
                pending.Push(child.Id);
            }
        }
    }
}
