namespace FlowGraph.Core.Commands;

/// <summary>
/// Command to add an edge to the graph.
/// </summary>
public class AddEdgeCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly Edge _edge;

    public string Description => "Add connection";

    public AddEdgeCommand(Graph graph, Edge edge)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _edge = edge ?? throw new ArgumentNullException(nameof(edge));
    }

    public void Execute()
    {
        if (!_graph.Edges.Any(e => e.Id == _edge.Id))
        {
            _graph.AddEdge(_edge);
        }
    }

    public void Undo()
    {
        _graph.RemoveEdge(_edge.Id);
    }
}

/// <summary>
/// Command to remove an edge from the graph.
/// </summary>
public class RemoveEdgeCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly Edge _edge;

    public string Description => "Remove connection";

    public RemoveEdgeCommand(Graph graph, Edge edge)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _edge = edge ?? throw new ArgumentNullException(nameof(edge));
    }

    public void Execute()
    {
        _graph.RemoveEdge(_edge.Id);
    }

    public void Undo()
    {
        if (!_graph.Edges.Any(e => e.Id == _edge.Id))
        {
            _graph.AddEdge(_edge);
        }
    }
}

/// <summary>
/// Command to remove multiple edges from the graph.
/// </summary>
public class RemoveEdgesCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly List<Edge> _edges;

    public string Description => _edges.Count == 1 
        ? "Remove connection" 
        : $"Remove {_edges.Count} connections";

    public RemoveEdgesCommand(Graph graph, IEnumerable<Edge> edges)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _edges = edges.ToList();
        
        if (_edges.Count == 0)
            throw new ArgumentException("At least one edge must be specified", nameof(edges));
    }

    public void Execute()
    {
        foreach (var edge in _edges)
        {
            _graph.RemoveEdge(edge.Id);
        }
    }

    public void Undo()
    {
        foreach (var edge in _edges)
        {
            if (!_graph.Edges.Any(e => e.Id == edge.Id))
            {
                _graph.AddEdge(edge);
            }
        }
    }
}
