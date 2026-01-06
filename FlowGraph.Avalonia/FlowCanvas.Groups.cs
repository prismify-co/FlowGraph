using FlowGraph.Core;
using FlowGraph.Core.Commands;

namespace FlowGraph.Avalonia;

/// <summary>
/// FlowCanvas partial - Group operations (grouping, collapsing, hierarchy).
/// </summary>
public partial class FlowCanvas
{
    #region Group Operations

    /// <summary>
    /// Groups the selected nodes into a new group.
    /// </summary>
    /// <param name="groupLabel">Optional label for the group.</param>
    /// <returns>The created group node, or null if grouping failed.</returns>
    public Node? GroupSelected(string? groupLabel = null)
    {
        if (Graph == null) return null;

        var selectedNodes = Graph.Nodes
            .Where(n => n.IsSelected && !n.IsGroup)
            .ToList();

        System.Diagnostics.Debug.WriteLine($"[GroupSelected] Selected nodes: {selectedNodes.Count} ({string.Join(", ", selectedNodes.Select(n => n.Id))})");

        if (selectedNodes.Count < 2)
        {
            System.Diagnostics.Debug.WriteLine($"[GroupSelected] Need at least 2 nodes, got {selectedNodes.Count}");
            return null;
        }

        var nodeIds = selectedNodes.Select(n => n.Id).ToList();
        var command = new GroupNodesCommand(Graph, nodeIds, groupLabel);
        CommandHistory.Execute(command);

        // Return the created group
        var createdGroup = Graph.Nodes.FirstOrDefault(n => n.IsGroup &&
            nodeIds.All(id => Graph.Nodes.FirstOrDefault(n2 => n2.Id == id)?.ParentGroupId == n.Id));
        
        System.Diagnostics.Debug.WriteLine($"[GroupSelected] Created group: {createdGroup?.Id ?? "null"}");
        
        return createdGroup;
    }

    /// <summary>
    /// Ungroups the selected group(s).
    /// </summary>
    public void UngroupSelected()
    {
        if (Graph == null) return;

        var selectedGroups = Graph.Nodes
            .Where(n => n.IsSelected && n.IsGroup)
            .ToList();

        if (selectedGroups.Count == 0) return;

        // Ungroup all selected groups
        var commands = selectedGroups
            .Select(g => new UngroupNodesCommand(Graph, g.Id))
            .Cast<IGraphCommand>()
            .ToList();

        if (commands.Count == 1)
        {
            CommandHistory.Execute(commands[0]);
        }
        else
        {
            CommandHistory.Execute(new CompositeCommand("Ungroup multiple groups", commands));
        }
    }

    /// <summary>
    /// Toggles the collapsed state of a group.
    /// </summary>
    /// <param name="groupId">The ID of the group to toggle.</param>
    public void ToggleGroupCollapse(string groupId)
    {
        _groupManager.ToggleCollapse(groupId);
    }

    /// <summary>
    /// Sets the collapsed state of a group.
    /// </summary>
    /// <param name="groupId">The ID of the group.</param>
    /// <param name="collapsed">True to collapse, false to expand.</param>
    public void SetGroupCollapsed(string groupId, bool collapsed)
    {
        _groupManager.SetCollapsed(groupId, collapsed);
    }

    /// <summary>
    /// Collapses all groups in the graph.
    /// </summary>
    public void CollapseAllGroups()
    {
        _groupManager.CollapseAll();
    }

    /// <summary>
    /// Expands all groups in the graph.
    /// </summary>
    public void ExpandAllGroups()
    {
        _groupManager.ExpandAll();
    }

    /// <summary>
    /// Adds nodes to an existing group.
    /// </summary>
    /// <param name="groupId">The ID of the target group.</param>
    /// <param name="nodeIds">The IDs of the nodes to add.</param>
    public void AddNodesToGroup(string groupId, IEnumerable<string> nodeIds)
    {
        _groupManager.AddNodesToGroup(groupId, nodeIds);
    }

    /// <summary>
    /// Adds selected nodes to the specified group.
    /// </summary>
    /// <param name="groupId">The ID of the target group.</param>
    public void AddSelectedToGroup(string groupId)
    {
        if (Graph == null) return;

        var selectedNodes = Graph.Nodes
            .Where(n => n.IsSelected && !n.IsGroup && n.Id != groupId)
            .Select(n => n.Id)
            .ToList();

        if (selectedNodes.Count > 0)
        {
            AddNodesToGroup(groupId, selectedNodes);
        }
    }

    /// <summary>
    /// Removes nodes from their parent group.
    /// </summary>
    /// <param name="nodeIds">The IDs of the nodes to remove from their groups.</param>
    public void RemoveNodesFromGroup(IEnumerable<string> nodeIds)
    {
        _groupManager.RemoveNodesFromGroup(nodeIds);
    }

    /// <summary>
    /// Removes selected nodes from their parent groups.
    /// </summary>
    public void RemoveSelectedFromGroups()
    {
        if (Graph == null) return;

        var selectedNodes = Graph.Nodes
            .Where(n => n.IsSelected && !string.IsNullOrEmpty(n.ParentGroupId))
            .Select(n => n.Id)
            .ToList();

        if (selectedNodes.Count > 0)
        {
            RemoveNodesFromGroup(selectedNodes);
        }
    }

    /// <summary>
    /// Auto-resizes a group to fit its children.
    /// </summary>
    /// <param name="groupId">The ID of the group to resize.</param>
    public void AutoResizeGroup(string groupId)
    {
        _groupManager.AutoResizeGroup(groupId);
    }

    /// <summary>
    /// Auto-resizes all groups to fit their children.
    /// </summary>
    public void AutoResizeAllGroups()
    {
        if (Graph == null) return;

        foreach (var group in Graph.Nodes.Where(n => n.IsGroup))
        {
            _groupManager.AutoResizeGroup(group.Id);
        }
    }

    /// <summary>
    /// Gets all groups in the graph.
    /// </summary>
    public IEnumerable<Node> GetAllGroups()
    {
        return _groupManager.GetAllGroups();
    }

    /// <summary>
    /// Gets the children of a specific group.
    /// </summary>
    /// <param name="groupId">The ID of the group.</param>
    public IEnumerable<Node> GetGroupChildren(string groupId)
    {
        return _groupManager.GetGroupChildren(groupId);
    }

    /// <summary>
    /// Checks if a node is visible (not hidden by a collapsed parent group).
    /// </summary>
    /// <param name="nodeId">The ID of the node to check.</param>
    public bool IsNodeVisible(string nodeId)
    {
        return _groupManager.IsNodeVisible(nodeId);
    }

    /// <summary>
    /// Gets all visible nodes (excluding those hidden by collapsed groups).
    /// </summary>
    public IEnumerable<Node> GetVisibleNodes()
    {
        return _groupManager.GetVisibleNodes();
    }

    /// <summary>
    /// Gets the group at a specific canvas point.
    /// </summary>
    /// <param name="canvasPoint">The point in canvas coordinates.</param>
    /// <returns>The group ID, or null if no group at that point.</returns>
    public string? GetGroupAtPoint(Core.Point canvasPoint)
    {
        return _groupManager.GetGroupAtPoint(canvasPoint);
    }

    #endregion
}
