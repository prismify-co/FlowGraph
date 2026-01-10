using FlowGraph.Core;
using FlowGraph.Core.Commands;

namespace FlowGraph.Avalonia;

/// <summary>
/// Manages group operations including auto-sizing, collapse state, and drag-to-add.
/// </summary>
public class GroupManager
{
    private readonly IGraphContext _context;
    private readonly CommandHistory _commandHistory;
    private readonly FlowCanvasSettings _settings;

    /// <summary>
    /// Padding around children when calculating group bounds.
    /// </summary>
    public double GroupPadding { get; set; } = 20;

    /// <summary>
    /// Height of the group header.
    /// </summary>
    public double HeaderHeight { get; set; } = 30;

    /// <summary>
    /// Event raised when a group's collapsed state changes.
    /// </summary>
    public event EventHandler<GroupCollapsedEventArgs>? GroupCollapsedChanged;

    /// <summary>
    /// Event raised when nodes are added to a group.
    /// </summary>
    public event EventHandler<NodesAddedToGroupEventArgs>? NodesAddedToGroup;

    /// <summary>
    /// Event raised when a group needs to be re-rendered.
    /// </summary>
    public event EventHandler<string>? GroupRerenderRequested;

    /// <summary>
    /// Creates a new group manager.
    /// </summary>
    /// <param name="context">The graph context providing access to the current graph.</param>
    /// <param name="commandHistory">The command history for undo/redo support.</param>
    /// <param name="settings">The canvas settings.</param>
    public GroupManager(
        IGraphContext context,
        CommandHistory commandHistory,
        FlowCanvasSettings settings)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _commandHistory = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    // Backwards compatibility constructor
    [Obsolete("Use the constructor that accepts IGraphContext instead.")]
    public GroupManager(
        Func<Graph?> getGraph,
        CommandHistory commandHistory,
        FlowCanvasSettings settings)
        : this(new FuncGraphContext(getGraph), commandHistory, settings)
    {
    }

    /// <summary>
    /// Creates a group from the specified nodes.
    /// </summary>
    public Node? CreateGroup(IEnumerable<string> nodeIds, string? label = null)
    {
        var graph = _context.Graph;
        if (graph == null) return null;

        var nodeIdList = nodeIds.ToList();
        if (nodeIdList.Count < 2) return null;

        var command = new GroupNodesCommand(graph, nodeIdList, label);
        _commandHistory.Execute(command);

        // Return the created group
        return graph.Elements.Nodes.FirstOrDefault(n => n.IsGroup &&
            nodeIdList.All(id => graph.Elements.Nodes.FirstOrDefault(n2 => n2.Id == id)?.ParentGroupId == n.Id));
    }

    /// <summary>
    /// Ungroups the specified group.
    /// </summary>
    public void Ungroup(string groupId)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        var command = new UngroupNodesCommand(graph, groupId);
        _commandHistory.Execute(command);
    }

    /// <summary>
    /// Toggles the collapsed state of a group.
    /// </summary>
    public void ToggleCollapse(string groupId)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        var group = graph.Elements.Nodes.FirstOrDefault(n => n.Id == groupId && n.IsGroup);
        if (group == null) return;

        var command = new ToggleGroupCollapseCommand(graph, groupId);
        _commandHistory.Execute(command);

        // Update visibility of child nodes
        UpdateChildVisibility(graph, groupId, group.IsCollapsed);

        GroupCollapsedChanged?.Invoke(this, new GroupCollapsedEventArgs(groupId, group.IsCollapsed));
    }

    /// <summary>
    /// Sets the collapsed state of a group.
    /// </summary>
    public void SetCollapsed(string groupId, bool collapsed)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        var group = graph.Elements.Nodes.FirstOrDefault(n => n.Id == groupId && n.IsGroup);
        if (group == null || group.IsCollapsed == collapsed) return;

        ToggleCollapse(groupId);
    }

    /// <summary>
    /// Adds nodes to an existing group.
    /// </summary>
    public void AddNodesToGroup(string groupId, IEnumerable<string> nodeIds)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        var nodeIdList = nodeIds.ToList();
        if (nodeIdList.Count == 0) return;

        var command = new AddNodesToGroupCommand(graph, groupId, nodeIdList);
        _commandHistory.Execute(command);

        // Auto-resize the group to fit new children
        AutoResizeGroup(groupId);

        NodesAddedToGroup?.Invoke(this, new NodesAddedToGroupEventArgs(groupId, nodeIdList));
    }

    /// <summary>
    /// Removes nodes from their parent group.
    /// </summary>
    public void RemoveNodesFromGroup(IEnumerable<string> nodeIds)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        var nodeIdList = nodeIds.ToList();
        if (nodeIdList.Count == 0) return;

        // Get affected groups for resize
        var affectedGroups = nodeIdList
            .Select(id => graph.Elements.Nodes.FirstOrDefault(n => n.Id == id)?.ParentGroupId)
            .Where(gid => !string.IsNullOrEmpty(gid))
            .Distinct()
            .ToList();

        var command = new RemoveNodesFromGroupCommand(graph, nodeIdList);
        _commandHistory.Execute(command);

        // Auto-resize affected groups
        foreach (var groupId in affectedGroups)
        {
            if (groupId != null)
                AutoResizeGroup(groupId);
        }
    }

    /// <summary>
    /// Auto-resizes a group to fit its children with padding.
    /// </summary>
    public void AutoResizeGroup(string groupId)
    {
        var graph = _context.Graph;
        if (graph == null) return;

        var group = graph.Elements.Nodes.FirstOrDefault(n => n.Id == groupId && n.IsGroup);
        if (group == null) return;

        var children = graph.GetGroupChildren(groupId).ToList();
        if (children.Count == 0)
        {
            // No children - set minimum size
            group.Width = 200;
            group.Height = 100;
            return;
        }

        var defaultWidth = _settings.NodeWidth;
        var defaultHeight = _settings.NodeHeight;

        var minX = children.Min(n => n.Position.X);
        var minY = children.Min(n => n.Position.Y);
        var maxX = children.Max(n => n.Position.X + (n.Width ?? defaultWidth));
        var maxY = children.Max(n => n.Position.Y + (n.Height ?? defaultHeight));

        // Update group position and size
        group.Position = new Point(minX - GroupPadding, minY - GroupPadding - HeaderHeight);
        group.Width = maxX - minX + GroupPadding * 2;
        group.Height = maxY - minY + GroupPadding * 2 + HeaderHeight;

        GroupRerenderRequested?.Invoke(this, groupId);
    }

    /// <summary>
    /// Checks if a point is inside a group's drop zone.
    /// </summary>
    public string? GetGroupAtPoint(Point canvasPoint)
    {
        var graph = _context.Graph;
        if (graph == null) return null;

        // Find groups that contain this point (check in reverse order for top-most)
        foreach (var group in graph.Elements.Nodes.Where(n => n.IsGroup && !n.IsCollapsed).Reverse())
        {
            var groupWidth = group.Width ?? 200;
            var groupHeight = group.Height ?? 100;

            if (canvasPoint.X >= group.Position.X &&
                canvasPoint.X <= group.Position.X + groupWidth &&
                canvasPoint.Y >= group.Position.Y &&
                canvasPoint.Y <= group.Position.Y + groupHeight)
            {
                return group.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// Handles dropping nodes onto a group.
    /// </summary>
    public bool HandleNodeDroppedOnGroup(IEnumerable<string> nodeIds, Point dropPoint)
    {
        var graph = _context.Graph;
        if (graph == null) return false;

        var targetGroupId = GetGroupAtPoint(dropPoint);
        if (targetGroupId == null) return false;

        var nodeIdList = nodeIds.ToList();

        // Filter out nodes that are already in this group or are the group itself
        var nodesToAdd = nodeIdList
            .Where(id => id != targetGroupId)
            .Where(id =>
            {
                var node = graph.Elements.Nodes.FirstOrDefault(n => n.Id == id);
                return node != null && node.ParentGroupId != targetGroupId;
            })
            .ToList();

        if (nodesToAdd.Count == 0) return false;

        AddNodesToGroup(targetGroupId, nodesToAdd);
        return true;
    }

    /// <summary>
    /// Collapses all groups in the graph.
    /// </summary>
    public void CollapseAll()
    {
        var graph = _context.Graph;
        if (graph == null) return;

        foreach (var group in graph.Elements.Nodes.Where(n => n.IsGroup && !n.IsCollapsed))
        {
            SetCollapsed(group.Id, true);
        }
    }

    /// <summary>
    /// Expands all groups in the graph.
    /// </summary>
    public void ExpandAll()
    {
        var graph = _context.Graph;
        if (graph == null) return;

        foreach (var group in graph.Elements.Nodes.Where(n => n.IsGroup && n.IsCollapsed))
        {
            SetCollapsed(group.Id, false);
        }
    }

    /// <summary>
    /// Gets all groups in the graph.
    /// </summary>
    public IEnumerable<Node> GetAllGroups()
    {
        var graph = _context.Graph;
        return graph?.GetGroups() ?? [];
    }

    /// <summary>
    /// Gets the children of a group.
    /// </summary>
    public IEnumerable<Node> GetGroupChildren(string groupId)
    {
        var graph = _context.Graph;
        return graph?.GetGroupChildren(groupId) ?? [];
    }

    /// <summary>
    /// Checks if a node is visible (not hidden by a collapsed parent group).
    /// </summary>
    public bool IsNodeVisible(string nodeId)
    {
        var graph = _context.Graph;
        if (graph == null) return false;

        var node = graph.Elements.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node == null) return false;

        // Check all ancestor groups - if any is collapsed, this node is hidden
        foreach (var ancestor in graph.GetAncestorGroups(node))
        {
            if (ancestor.IsCollapsed)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets all visible nodes (excluding those hidden by collapsed groups).
    /// </summary>
    public IEnumerable<Node> GetVisibleNodes()
    {
        var graph = _context.Graph;
        if (graph == null) yield break;

        foreach (var node in graph.Elements.Nodes)
        {
            if (IsNodeVisible(node.Id))
                yield return node;
        }
    }

    /// <summary>
    /// Gets all visible edges (excluding those with hidden endpoints).
    /// </summary>
    public IEnumerable<Edge> GetVisibleEdges()
    {
        var graph = _context.Graph;
        if (graph == null) yield break;

        foreach (var edge in graph.Elements.Edges)
        {
            if (IsNodeVisible(edge.Source) && IsNodeVisible(edge.Target))
                yield return edge;
        }
    }

    private void UpdateChildVisibility(Graph graph, string groupId, bool isCollapsed)
    {
        // This is a notification mechanism - actual visibility is handled by GetVisibleNodes
        // The renderer should check visibility when rendering
    }
}

/// <summary>
/// Event args for group collapsed state change.
/// </summary>
public class GroupCollapsedEventArgs : EventArgs
{
    public string GroupId { get; }
    public bool IsCollapsed { get; }

    public GroupCollapsedEventArgs(string groupId, bool isCollapsed)
    {
        GroupId = groupId;
        IsCollapsed = isCollapsed;
    }
}

/// <summary>
/// Event args for nodes added to group.
/// </summary>
public class NodesAddedToGroupEventArgs : EventArgs
{
    public string GroupId { get; }
    public IReadOnlyList<string> NodeIds { get; }

    public NodesAddedToGroupEventArgs(string groupId, IReadOnlyList<string> nodeIds)
    {
        GroupId = groupId;
        NodeIds = nodeIds;
    }
}
