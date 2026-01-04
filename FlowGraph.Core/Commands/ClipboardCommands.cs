namespace FlowGraph.Core.Commands;

/// <summary>
/// Command to paste nodes and edges from clipboard.
/// </summary>
public class PasteCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly List<Node> _pastedNodes;
    private readonly List<Edge> _pastedEdges;

    public string Description => _pastedNodes.Count == 1 
        ? "Paste node" 
        : $"Paste {_pastedNodes.Count} nodes";

    public PasteCommand(Graph graph, List<Node> pastedNodes, List<Edge> pastedEdges)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _pastedNodes = pastedNodes;
        _pastedEdges = pastedEdges;
    }

    public void Execute()
    {
        // Nodes and edges are added by ClipboardManager before this command is created
        // This is an "already executed" pattern - Execute does nothing on first call
    }

    public void Undo()
    {
        // Remove pasted edges first (they reference the nodes)
        foreach (var edge in _pastedEdges)
        {
            _graph.RemoveEdge(edge.Id);
        }

        // Remove pasted nodes
        foreach (var node in _pastedNodes)
        {
            _graph.RemoveNode(node.Id);
        }
    }
}

/// <summary>
/// Command to duplicate nodes and edges.
/// </summary>
public class DuplicateCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly List<Node> _duplicatedNodes;
    private readonly List<Edge> _duplicatedEdges;

    public string Description => _duplicatedNodes.Count == 1 
        ? "Duplicate node" 
        : $"Duplicate {_duplicatedNodes.Count} nodes";

    public DuplicateCommand(Graph graph, List<Node> duplicatedNodes, List<Edge> duplicatedEdges)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _duplicatedNodes = duplicatedNodes;
        _duplicatedEdges = duplicatedEdges;
    }

    public void Execute()
    {
        // Nodes and edges are added by ClipboardManager before this command is created
    }

    public void Undo()
    {
        foreach (var edge in _duplicatedEdges)
        {
            _graph.RemoveEdge(edge.Id);
        }

        foreach (var node in _duplicatedNodes)
        {
            _graph.RemoveNode(node.Id);
        }
    }
}
