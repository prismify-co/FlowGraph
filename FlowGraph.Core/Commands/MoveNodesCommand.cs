namespace FlowGraph.Core.Commands;

/// <summary>
/// Command to move one or more nodes to new positions.
/// </summary>
public class MoveNodesCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly Dictionary<string, Point> _oldPositions;
    private readonly Dictionary<string, Point> _newPositions;

    public string Description { get; }

    /// <summary>
    /// Creates a move command for a single node.
    /// </summary>
    public MoveNodesCommand(Graph graph, Node node, Point oldPosition, Point newPosition)
        : this(graph, 
               new Dictionary<string, Point> { { node.Id, oldPosition } },
               new Dictionary<string, Point> { { node.Id, newPosition } })
    {
    }

    /// <summary>
    /// Creates a move command for multiple nodes.
    /// </summary>
    public MoveNodesCommand(
        Graph graph,
        Dictionary<string, Point> oldPositions,
        Dictionary<string, Point> newPositions)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _oldPositions = new Dictionary<string, Point>(oldPositions);
        _newPositions = new Dictionary<string, Point>(newPositions);

        Description = _oldPositions.Count == 1 
            ? "Move node" 
            : $"Move {_oldPositions.Count} nodes";
    }

    public void Execute()
    {
        foreach (var (nodeId, newPos) in _newPositions)
        {
            var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.Position = newPos;
            }
        }
    }

    public void Undo()
    {
        foreach (var (nodeId, oldPos) in _oldPositions)
        {
            var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.Position = oldPos;
            }
        }
    }
}
