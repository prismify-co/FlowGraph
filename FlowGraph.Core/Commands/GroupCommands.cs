namespace FlowGraph.Core.Commands;

/// <summary>
/// Command to group selected nodes into a new group.
/// </summary>
public class GroupNodesCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly List<string> _nodeIds;
    private readonly string _groupId;
    private readonly string? _groupLabel;
    private readonly Dictionary<string, string?> _previousParentGroupIds = new();
    private Node? _createdGroup;
    private Point _groupPosition;
    private double _groupWidth;
    private double _groupHeight;

    // Default node dimensions - should match FlowCanvasSettings defaults
    private const double DefaultNodeWidth = 150;
    private const double DefaultNodeHeight = 80;
    private const double GroupPadding = 20;
    private const double GroupHeaderHeight = 30;

    public string Description => _nodeIds.Count == 1 
        ? "Group node" 
        : $"Group {_nodeIds.Count} nodes";

    public GroupNodesCommand(Graph graph, IEnumerable<string> nodeIds, string? groupLabel = null)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _nodeIds = nodeIds.ToList();
        _groupId = Guid.NewGuid().ToString();
        _groupLabel = groupLabel;
    }

    public void Execute()
    {
        if (_nodeIds.Count == 0)
            return;

        // Store previous parent group IDs for undo
        foreach (var nodeId in _nodeIds)
        {
            var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                _previousParentGroupIds[nodeId] = node.ParentGroupId;
            }
        }

        // Calculate group bounds based on child nodes
        var nodes = _nodeIds
            .Select(id => _graph.Nodes.FirstOrDefault(n => n.Id == id))
            .Where(n => n != null)
            .Cast<Node>()
            .ToList();

        if (nodes.Count == 0)
            return;

        // Calculate bounds using actual node dimensions
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var node in nodes)
        {
            var nodeWidth = node.Width ?? DefaultNodeWidth;
            var nodeHeight = node.Height ?? DefaultNodeHeight;

            minX = Math.Min(minX, node.Position.X);
            minY = Math.Min(minY, node.Position.Y);
            maxX = Math.Max(maxX, node.Position.X + nodeWidth);
            maxY = Math.Max(maxY, node.Position.Y + nodeHeight);
        }

        _groupPosition = new Point(minX - GroupPadding, minY - GroupPadding - GroupHeaderHeight);
        _groupWidth = maxX - minX + GroupPadding * 2;
        _groupHeight = maxY - minY + GroupPadding * 2 + GroupHeaderHeight;

        // Create the group node
        _createdGroup = new Node
        {
            Id = _groupId,
            Type = "group",
            IsGroup = true,
            Label = _groupLabel ?? "Group",
            Position = _groupPosition,
            Width = _groupWidth,
            Height = _groupHeight,
            IsResizable = true
        };

        // Add group to graph first
        _graph.AddNode(_createdGroup);

        // Update parent group ID for all selected nodes
        foreach (var node in nodes)
        {
            node.ParentGroupId = _groupId;
        }
    }

    public void Undo()
    {
        // Restore previous parent group IDs
        foreach (var (nodeId, previousParentId) in _previousParentGroupIds)
        {
            var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.ParentGroupId = previousParentId;
            }
        }

        // Remove the created group
        if (_createdGroup != null)
        {
            _graph.RemoveNode(_groupId);
            _createdGroup = null;
        }
    }
}

/// <summary>
/// Command to ungroup nodes from a group.
/// </summary>
public class UngroupNodesCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly string _groupId;
    private Node? _removedGroup;
    private readonly Dictionary<string, string?> _previousParentGroupIds = new();

    public string Description => "Ungroup nodes";

    public UngroupNodesCommand(Graph graph, string groupId)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _groupId = groupId;
    }

    public void Execute()
    {
        var group = _graph.Nodes.FirstOrDefault(n => n.Id == _groupId && n.IsGroup);
        if (group == null)
            return;

        _removedGroup = group;

        // Get the group's parent (if nested)
        var groupParentId = group.ParentGroupId;

        // Get all direct children of this group
        var children = _graph.GetGroupChildren(_groupId).ToList();

        // Store previous state and update children's parent to the group's parent
        foreach (var child in children)
        {
            _previousParentGroupIds[child.Id] = child.ParentGroupId;
            child.ParentGroupId = groupParentId;
        }

        // Remove the group node
        _graph.RemoveNode(_groupId);
    }

    public void Undo()
    {
        // Re-add the group node
        if (_removedGroup != null)
        {
            _graph.AddNode(_removedGroup);

            // Restore children's parent group IDs
            foreach (var (nodeId, previousParentId) in _previousParentGroupIds)
            {
                var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node != null)
                {
                    node.ParentGroupId = previousParentId;
                }
            }
        }
    }
}

/// <summary>
/// Command to toggle a group's collapsed state.
/// </summary>
public class ToggleGroupCollapseCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly string _groupId;
    private bool _previousState;

    public string Description { get; private set; } = "Toggle group collapse";

    public ToggleGroupCollapseCommand(Graph graph, string groupId)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _groupId = groupId;
    }

    public void Execute()
    {
        var group = _graph.Nodes.FirstOrDefault(n => n.Id == _groupId && n.IsGroup);
        if (group == null)
            return;

        _previousState = group.IsCollapsed;
        group.IsCollapsed = !group.IsCollapsed;
        Description = group.IsCollapsed ? "Collapse group" : "Expand group";
    }

    public void Undo()
    {
        var group = _graph.Nodes.FirstOrDefault(n => n.Id == _groupId && n.IsGroup);
        if (group != null)
        {
            group.IsCollapsed = _previousState;
        }
    }
}

/// <summary>
/// Command to add nodes to an existing group.
/// </summary>
public class AddNodesToGroupCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly string _groupId;
    private readonly List<string> _nodeIds;
    private readonly Dictionary<string, string?> _previousParentGroupIds = new();

    public string Description => _nodeIds.Count == 1 
        ? "Add node to group" 
        : $"Add {_nodeIds.Count} nodes to group";

    public AddNodesToGroupCommand(Graph graph, string groupId, IEnumerable<string> nodeIds)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _groupId = groupId;
        _nodeIds = nodeIds.ToList();
    }

    public void Execute()
    {
        var group = _graph.Nodes.FirstOrDefault(n => n.Id == _groupId && n.IsGroup);
        if (group == null)
            return;

        foreach (var nodeId in _nodeIds)
        {
            var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null && node.Id != _groupId)
            {
                _previousParentGroupIds[nodeId] = node.ParentGroupId;
                node.ParentGroupId = _groupId;
            }
        }
    }

    public void Undo()
    {
        foreach (var (nodeId, previousParentId) in _previousParentGroupIds)
        {
            var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.ParentGroupId = previousParentId;
            }
        }
    }
}

/// <summary>
/// Command to remove nodes from their parent group.
/// </summary>
public class RemoveNodesFromGroupCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly List<string> _nodeIds;
    private readonly Dictionary<string, string?> _previousParentGroupIds = new();

    public string Description => _nodeIds.Count == 1 
        ? "Remove node from group" 
        : $"Remove {_nodeIds.Count} nodes from group";

    public RemoveNodesFromGroupCommand(Graph graph, IEnumerable<string> nodeIds)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _nodeIds = nodeIds.ToList();
    }

    public void Execute()
    {
        foreach (var nodeId in _nodeIds)
        {
            var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null && !string.IsNullOrEmpty(node.ParentGroupId))
            {
                _previousParentGroupIds[nodeId] = node.ParentGroupId;
                
                // Get the group's parent to handle nested groups
                var group = _graph.Nodes.FirstOrDefault(n => n.Id == node.ParentGroupId);
                node.ParentGroupId = group?.ParentGroupId;
            }
        }
    }

    public void Undo()
    {
        foreach (var (nodeId, previousParentId) in _previousParentGroupIds)
        {
            var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.ParentGroupId = previousParentId;
            }
        }
    }
}
