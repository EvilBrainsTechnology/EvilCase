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
    /// A node always follows the one it hangs under, so one pass places the lot however deep it goes. A
    /// node whose parent never arrived stays out.
    /// </summary>
    public static CaseTreeBranch Nest(CaseTreeNode root, IReadOnlyList<CaseTreeNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var childrenOf = new Dictionary<long, List<CaseTreeBranch>>();
        var tree = Branch(root, childrenOf);

        foreach (var node in nodes)
        {
            if (childrenOf.TryGetValue(node.ParentId, out var siblings))
                siblings.Add(Branch(node, childrenOf));
        }

        return tree;
    }

    private static CaseTreeBranch Branch(CaseTreeNode node, Dictionary<long, List<CaseTreeBranch>> childrenOf)
    {
        var children = new List<CaseTreeBranch>();
        childrenOf[node.Id] = children;

        return new CaseTreeBranch { Node = node, Children = children };
    }
}
