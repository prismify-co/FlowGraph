namespace FlowGraph.Core;

/// <summary>
/// Extension methods for working with node groups in a graph.
/// </summary>
public static class GraphGroupExtensions
{
    /// <summary>
    /// Gets all nodes that are children of the specified group.
    /// </summary>
    public static IEnumerable<Node> GetGroupChildren(this Graph graph, string groupId)
    {
        return graph.Elements.Nodes.Where(n => n.ParentGroupId == groupId);
    }

    /// <summary>
    /// Gets all nodes that are children of the specified group, recursively.
    /// </summary>
    public static IEnumerable<Node> GetGroupChildrenRecursive(this Graph graph, string groupId)
    {
        var children = graph.GetGroupChildren(groupId).ToList();
        var result = new List<Node>(children);

        foreach (var child in children.Where(c => c.IsGroup))
        {
            result.AddRange(graph.GetGroupChildrenRecursive(child.Id));
        }

        return result;
    }

    /// <summary>
    /// Gets the parent group node for a node, if any.
    /// </summary>
    public static Node? GetParentGroup(this Graph graph, Node node)
    {
        if (string.IsNullOrEmpty(node.ParentGroupId))
            return null;

        return graph.Elements.Nodes.FirstOrDefault(n => n.Id == node.ParentGroupId && n.IsGroup);
    }

    /// <summary>
    /// Gets all ancestor group nodes for a node.
    /// </summary>
    public static IEnumerable<Node> GetAncestorGroups(this Graph graph, Node node)
    {
        var current = graph.GetParentGroup(node);
        while (current != null)
        {
            yield return current;
            current = graph.GetParentGroup(current);
        }
    }

    /// <summary>
    /// Checks if a node is a descendant of a group.
    /// </summary>
    public static bool IsDescendantOf(this Graph graph, Node node, string groupId)
    {
        return graph.GetAncestorGroups(node).Any(g => g.Id == groupId);
    }

    /// <summary>
    /// Gets all group nodes in the graph.
    /// </summary>
    public static IEnumerable<Node> GetGroups(this Graph graph)
    {
        return graph.Elements.Nodes.Where(n => n.IsGroup);
    }

    /// <summary>
    /// Gets top-level nodes (nodes without a parent group).
    /// </summary>
    public static IEnumerable<Node> GetTopLevelNodes(this Graph graph)
    {
        return graph.Elements.Nodes.Where(n => string.IsNullOrEmpty(n.ParentGroupId));
    }

    /// <summary>
    /// Calculates the bounding box of a group based on its children.
    /// </summary>
    /// <param name="graph">The graph containing the group.</param>
    /// <param name="groupId">The group ID.</param>
    /// <param name="padding">Padding around children. If null, uses the group's GroupPadding property or <see cref="GraphDefaults.GroupPadding"/>.</param>
    /// <param name="headerHeight">Height of group header. Defaults to <see cref="GraphDefaults.GroupHeaderHeight"/>.</param>
    /// <param name="defaultNodeWidth">Default node width. Defaults to <see cref="GraphDefaults.NodeWidth"/>.</param>
    /// <param name="defaultNodeHeight">Default node height. Defaults to <see cref="GraphDefaults.NodeHeight"/>.</param>
    public static (Point topLeft, double width, double height) CalculateGroupBounds(
        this Graph graph,
        string groupId,
        double? padding = null,
        double headerHeight = GraphDefaults.GroupHeaderHeight,
        double defaultNodeWidth = GraphDefaults.NodeWidth,
        double defaultNodeHeight = GraphDefaults.NodeHeight)
    {
        var group = graph.FindNode(groupId);
        var children = graph.GetGroupChildren(groupId).ToList();

        if (children.Count == 0)
        {
            return (new Point(0, 0), GraphDefaults.GroupMinWidth, GraphDefaults.GroupMinHeight);
        }

        // Use provided padding, then per-group padding, then global default
        var effectivePadding = padding ?? group?.GroupPadding ?? GraphDefaults.GroupPadding;

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var child in children)
        {
            var nodeWidth = child.Width ?? defaultNodeWidth;
            var nodeHeight = child.Height ?? defaultNodeHeight;

            minX = Math.Min(minX, child.Position.X);
            minY = Math.Min(minY, child.Position.Y);
            maxX = Math.Max(maxX, child.Position.X + nodeWidth);
            maxY = Math.Max(maxY, child.Position.Y + nodeHeight);
        }

        return (
            new Point(minX - effectivePadding, minY - effectivePadding - headerHeight),
            maxX - minX + effectivePadding * 2,
            maxY - minY + effectivePadding * 2 + headerHeight
        );
    }

    /// <summary>
    /// Checks if an edge is internal to a group (both endpoints in the same group).
    /// </summary>
    public static bool IsEdgeInternalToGroup(this Graph graph, Edge edge, string groupId)
    {
        var sourceNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return false;

        var sourceInGroup = sourceNode.ParentGroupId == groupId ||
                           graph.IsDescendantOf(sourceNode, groupId);
        var targetInGroup = targetNode.ParentGroupId == groupId ||
                           graph.IsDescendantOf(targetNode, groupId);

        return sourceInGroup && targetInGroup;
    }

    /// <summary>
    /// Checks if an edge crosses a group boundary (one end inside, one outside).
    /// </summary>
    public static bool IsEdgeCrossingGroup(this Graph graph, Edge edge, string groupId)
    {
        var sourceNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Source);
        var targetNode = graph.Elements.Nodes.FirstOrDefault(n => n.Id == edge.Target);

        if (sourceNode == null || targetNode == null)
            return false;

        var sourceInGroup = sourceNode.ParentGroupId == groupId ||
                           graph.IsDescendantOf(sourceNode, groupId);
        var targetInGroup = targetNode.ParentGroupId == groupId ||
                           graph.IsDescendantOf(targetNode, groupId);

        return sourceInGroup != targetInGroup;
    }

    /// <summary>
    /// Gets all edges that cross a group boundary.
    /// </summary>
    public static IEnumerable<Edge> GetEdgesCrossingGroup(this Graph graph, string groupId)
    {
        return graph.Elements.Edges.Where(e => graph.IsEdgeCrossingGroup(e, groupId));
    }

    /// <summary>
    /// Gets all edges that are internal to a group.
    /// </summary>
    public static IEnumerable<Edge> GetEdgesInternalToGroup(this Graph graph, string groupId)
    {
        return graph.Elements.Edges.Where(e => graph.IsEdgeInternalToGroup(e, groupId));
    }
}
