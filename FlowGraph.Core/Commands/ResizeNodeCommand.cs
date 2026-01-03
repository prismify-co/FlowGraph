namespace FlowGraph.Core.Commands;

/// <summary>
/// Command to resize a node.
/// </summary>
public class ResizeNodeCommand : IGraphCommand
{
    private readonly Graph _graph;
    private readonly string _nodeId;
    private readonly double? _oldWidth;
    private readonly double? _oldHeight;
    private readonly double? _newWidth;
    private readonly double? _newHeight;
    private readonly Point _oldPosition;
    private readonly Point _newPosition;

    public string Description => "Resize node";

    /// <summary>
    /// Creates a resize command for a node.
    /// </summary>
    /// <param name="graph">The graph containing the node.</param>
    /// <param name="nodeId">The ID of the node to resize.</param>
    /// <param name="oldWidth">The old width (null = default).</param>
    /// <param name="oldHeight">The old height (null = default).</param>
    /// <param name="newWidth">The new width (null = default).</param>
    /// <param name="newHeight">The new height (null = default).</param>
    /// <param name="oldPosition">The old position (for corner resizing).</param>
    /// <param name="newPosition">The new position (for corner resizing).</param>
    public ResizeNodeCommand(
        Graph graph,
        string nodeId,
        double? oldWidth,
        double? oldHeight,
        double? newWidth,
        double? newHeight,
        Point oldPosition,
        Point newPosition)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _nodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        _oldWidth = oldWidth;
        _oldHeight = oldHeight;
        _newWidth = newWidth;
        _newHeight = newHeight;
        _oldPosition = oldPosition;
        _newPosition = newPosition;
    }

    /// <summary>
    /// Creates a resize command for a node (without position change).
    /// </summary>
    public ResizeNodeCommand(
        Graph graph,
        string nodeId,
        double? oldWidth,
        double? oldHeight,
        double? newWidth,
        double? newHeight)
        : this(graph, nodeId, oldWidth, oldHeight, newWidth, newHeight, 
               new Point(0, 0), new Point(0, 0))
    {
        // Get current position for the node
        var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node != null)
        {
            _oldPosition = node.Position;
            _newPosition = node.Position;
        }
    }

    public void Execute()
    {
        var node = _graph.Nodes.FirstOrDefault(n => n.Id == _nodeId);
        if (node != null)
        {
            node.Width = _newWidth;
            node.Height = _newHeight;
            node.Position = _newPosition;
        }
    }

    public void Undo()
    {
        var node = _graph.Nodes.FirstOrDefault(n => n.Id == _nodeId);
        if (node != null)
        {
            node.Width = _oldWidth;
            node.Height = _oldHeight;
            node.Position = _oldPosition;
        }
    }
}
