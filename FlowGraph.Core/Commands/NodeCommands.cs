namespace FlowGraph.Core.Commands;

/// <summary>
/// Command to add a node to the graph.
/// </summary>
public class AddNodeCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly Node _node;

    public string Description => $"Add {_node.Type} node";

    public AddNodeCommand(Graph graph, Node node)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _node = node ?? throw new ArgumentNullException(nameof(node));
    }

    public void Execute()
    {
        if (!_graph.Nodes.Any(n => n.Id == _node.Id))
        {
            _graph.AddNode(_node);
        }
    }

    public void Undo()
    {
        _graph.RemoveNode(_node.Id);
    }
}

/// <summary>
/// Command to remove a node and its connected edges from the graph.
/// </summary>
public class RemoveNodeCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly Node _node;
    private readonly List<Edge> _connectedEdges;

    public string Description => $"Remove {_node.Type} node";

    public RemoveNodeCommand(Graph graph, Node node)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _node = node ?? throw new ArgumentNullException(nameof(node));
        
        // Store connected edges so we can restore them on undo
        _connectedEdges = _graph.Edges
            .Where(e => e.Source == node.Id || e.Target == node.Id)
            .ToList();
    }

    public void Execute()
    {
        _graph.RemoveNode(_node.Id);
    }

    public void Undo()
    {
        // Re-add the node
        if (!_graph.Nodes.Any(n => n.Id == _node.Id))
        {
            _graph.AddNode(_node);
        }

        // Re-add connected edges
        foreach (var edge in _connectedEdges)
        {
            if (!_graph.Edges.Any(e => e.Id == edge.Id))
            {
                _graph.AddEdge(edge);
            }
        }
    }
}

/// <summary>
/// Command to remove multiple nodes and their connected edges.
/// </summary>
public class RemoveNodesCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly List<Node> _nodes;
    private readonly List<Edge> _connectedEdges;

    public string Description => _nodes.Count == 1 
        ? $"Remove {_nodes[0].Type} node" 
        : $"Remove {_nodes.Count} nodes";

    public RemoveNodesCommand(Graph graph, IEnumerable<Node> nodes)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _nodes = nodes.ToList();
        
        if (_nodes.Count == 0)
            throw new ArgumentException("At least one node must be specified", nameof(nodes));

        // Store connected edges
        var nodeIds = new HashSet<string>(_nodes.Select(n => n.Id));
        _connectedEdges = _graph.Edges
            .Where(e => nodeIds.Contains(e.Source) || nodeIds.Contains(e.Target))
            .ToList();
    }

    public void Execute()
    {
        foreach (var node in _nodes)
        {
            _graph.RemoveNode(node.Id);
        }
    }

    public void Undo()
    {
        // Re-add nodes
        foreach (var node in _nodes)
        {
            if (!_graph.Nodes.Any(n => n.Id == node.Id))
            {
                _graph.AddNode(node);
            }
        }

        // Re-add connected edges
        foreach (var edge in _connectedEdges)
        {
            if (!_graph.Edges.Any(e => e.Id == edge.Id))
            {
                _graph.AddEdge(edge);
            }
        }
    }
}
