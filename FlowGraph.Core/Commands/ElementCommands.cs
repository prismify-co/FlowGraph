using FlowGraph.Core.Elements;
using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Core.Commands;

/// <summary>
/// Command to move one or more elements (nodes, shapes) to new positions.
/// Works with any ICanvasElement that has a settable Position property.
/// </summary>
public class MoveElementsCommand : IGraphCommand
{
  private readonly Graph _graph;
  private readonly Dictionary<string, Point> _oldPositions;
  private readonly Dictionary<string, Point> _newPositions;

  public string Description { get; }

  /// <summary>
  /// Creates a move command for a single element.
  /// </summary>
  public MoveElementsCommand(Graph graph, ICanvasElement element, Point oldPosition, Point newPosition)
      : this(graph,
             new Dictionary<string, Point> { { element.Id, oldPosition } },
             new Dictionary<string, Point> { { element.Id, newPosition } })
  {
  }

  /// <summary>
  /// Creates a move command for multiple elements.
  /// </summary>
  public MoveElementsCommand(
      Graph graph,
      Dictionary<string, Point> oldPositions,
      Dictionary<string, Point> newPositions)
  {
    _graph = graph ?? throw new ArgumentNullException(nameof(graph));

    ArgumentNullException.ThrowIfNull(oldPositions);
    ArgumentNullException.ThrowIfNull(newPositions);

    if (oldPositions.Count == 0)
      throw new ArgumentException("At least one element position must be specified", nameof(oldPositions));

    if (oldPositions.Count != newPositions.Count)
      throw new ArgumentException("Old and new position dictionaries must have the same count", nameof(newPositions));

    _oldPositions = new Dictionary<string, Point>(oldPositions);
    _newPositions = new Dictionary<string, Point>(newPositions);

    Description = _oldPositions.Count == 1
        ? "Move element"
        : $"Move {_oldPositions.Count} elements";
  }

  public void Execute()
  {
    foreach (var (elementId, newPos) in _newPositions)
    {
      var element = _graph.Elements.FirstOrDefault(e => e.Id == elementId);
      if (element != null)
      {
        element.Position = newPos;
      }
    }
  }

  public void Undo()
  {
    foreach (var (elementId, oldPos) in _oldPositions)
    {
      var element = _graph.Elements.FirstOrDefault(e => e.Id == elementId);
      if (element != null)
      {
        element.Position = oldPos;
      }
    }
  }
}

/// <summary>
/// Command to resize one or more elements (nodes, shapes).
/// Works with any ICanvasElement that has settable Width/Height properties.
/// </summary>
public class ResizeElementsCommand : IGraphCommand
{
  private readonly Graph _graph;
  private readonly Dictionary<string, SizeSnapshot> _oldSizes;
  private readonly Dictionary<string, SizeSnapshot> _newSizes;

  public string Description { get; }

  /// <summary>
  /// Snapshot of an element's size and position for resize operations.
  /// </summary>
  public record SizeSnapshot(double? Width, double? Height, Point Position);

  /// <summary>
  /// Creates a resize command for a single element.
  /// </summary>
  public ResizeElementsCommand(
      Graph graph,
      ICanvasElement element,
      SizeSnapshot oldSize,
      SizeSnapshot newSize)
      : this(graph,
             new Dictionary<string, SizeSnapshot> { { element.Id, oldSize } },
             new Dictionary<string, SizeSnapshot> { { element.Id, newSize } })
  {
  }

  /// <summary>
  /// Creates a resize command for multiple elements.
  /// </summary>
  public ResizeElementsCommand(
      Graph graph,
      Dictionary<string, SizeSnapshot> oldSizes,
      Dictionary<string, SizeSnapshot> newSizes)
  {
    _graph = graph ?? throw new ArgumentNullException(nameof(graph));

    ArgumentNullException.ThrowIfNull(oldSizes);
    ArgumentNullException.ThrowIfNull(newSizes);

    if (oldSizes.Count == 0)
      throw new ArgumentException("At least one element size must be specified", nameof(oldSizes));

    if (oldSizes.Count != newSizes.Count)
      throw new ArgumentException("Old and new size dictionaries must have the same count", nameof(newSizes));

    _oldSizes = new Dictionary<string, SizeSnapshot>(oldSizes);
    _newSizes = new Dictionary<string, SizeSnapshot>(newSizes);

    Description = _oldSizes.Count == 1
        ? "Resize element"
        : $"Resize {_oldSizes.Count} elements";
  }

  public void Execute()
  {
    foreach (var (elementId, newSize) in _newSizes)
    {
      var element = _graph.Elements.FirstOrDefault(e => e.Id == elementId);
      if (element != null)
      {
        element.Width = newSize.Width;
        element.Height = newSize.Height;
        element.Position = newSize.Position;
      }
    }
  }

  public void Undo()
  {
    foreach (var (elementId, oldSize) in _oldSizes)
    {
      var element = _graph.Elements.FirstOrDefault(e => e.Id == elementId);
      if (element != null)
      {
        element.Width = oldSize.Width;
        element.Height = oldSize.Height;
        element.Position = oldSize.Position;
      }
    }
  }
}

/// <summary>
/// Command to remove one or more elements from the graph.
/// Supports nodes, edges, and shapes. For nodes, automatically removes connected edges.
/// </summary>
public class RemoveElementsCommand : IGraphCommand
{
  private readonly Graph _graph;
  private readonly List<ICanvasElement> _elements;
  private readonly List<Edge> _connectedEdges;

  public string Description { get; }

  /// <summary>
  /// Creates a remove command for a single element.
  /// </summary>
  public RemoveElementsCommand(Graph graph, ICanvasElement element)
      : this(graph, new[] { element })
  {
  }

  /// <summary>
  /// Creates a remove command for multiple elements.
  /// </summary>
  public RemoveElementsCommand(Graph graph, IEnumerable<ICanvasElement> elements)
  {
    _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    _elements = elements.ToList();

    if (_elements.Count == 0)
      throw new ArgumentException("At least one element must be specified", nameof(elements));

    // For nodes, store connected edges so we can restore them on undo
    var nodeIds = _elements.OfType<Node>().Select(n => n.Id).ToHashSet();
    _connectedEdges = _graph.Elements.Edges
        .Where(e => nodeIds.Contains(e.Source) || nodeIds.Contains(e.Target))
        .Where(e => !_elements.OfType<Edge>().Any(removed => removed.Id == e.Id)) // Don't double-count edges being explicitly removed
        .ToList();

    // Build description
    var nodeCount = _elements.OfType<Node>().Count();
    var edgeCount = _elements.OfType<Edge>().Count();
    var shapeCount = _elements.OfType<ShapeElement>().Count();

    if (_elements.Count == 1)
    {
      var element = _elements[0];
      Description = element switch
      {
        Node n => $"Remove {n.Type} node",
        Edge => "Remove edge",
        ShapeElement s => $"Remove {s.Type} shape",
        _ => "Remove element"
      };
    }
    else
    {
      var parts = new List<string>();
      if (nodeCount > 0) parts.Add($"{nodeCount} node{(nodeCount > 1 ? "s" : "")}");
      if (edgeCount > 0) parts.Add($"{edgeCount} edge{(edgeCount > 1 ? "s" : "")}");
      if (shapeCount > 0) parts.Add($"{shapeCount} shape{(shapeCount > 1 ? "s" : "")}");
      Description = $"Remove {string.Join(", ", parts)}";
    }
  }

  public void Execute()
  {
    foreach (var element in _elements)
    {
      _graph.RemoveElement(element);
    }
  }

  public void Undo()
  {
    // Re-add all removed elements
    foreach (var element in _elements)
    {
      if (!_graph.Elements.Any(e => e.Id == element.Id))
      {
        _graph.AddElement(element);
      }
    }

    // Re-add connected edges that were implicitly removed with nodes
    foreach (var edge in _connectedEdges)
    {
      if (!_graph.Elements.Edges.Any(e => e.Id == edge.Id))
      {
        _graph.AddEdge(edge);
      }
    }
  }
}
