using FlowGraph.Core.Elements.Shapes;

namespace FlowGraph.Core.Elements;

/// <summary>
/// Unified hit test result for all canvas elements.
/// Used for standardized tagging of controls and hit test payloads across both
/// visual-tree and direct-rendering modes.
/// </summary>
/// <remarks>
/// <para>
/// This replaces disparate tagging approaches:
/// - Control.Tag = Node
/// - Control.Tag = Edge
/// - Control.Tag = (Node, Port, bool)
/// </para>
/// <para>
/// With a single unified payload that supports all element types and handles.
/// </para>
/// </remarks>
public sealed class ElementHitInfo
{
  /// <summary>
  /// Creates a hit info for a simple element (node, edge, shape).
  /// </summary>
  public ElementHitInfo(ICanvasElement element)
  {
    Element = element ?? throw new ArgumentNullException(nameof(element));
  }

  /// <summary>
  /// Creates a hit info for an element with a handle (e.g., resize handle).
  /// </summary>
  public ElementHitInfo(ICanvasElement element, string handle)
  {
    Element = element ?? throw new ArgumentNullException(nameof(element));
    Handle = handle;
  }

  /// <summary>
  /// Creates a hit info for a node port.
  /// </summary>
  public ElementHitInfo(Node node, string portId, bool isOutput)
  {
    Element = node ?? throw new ArgumentNullException(nameof(node));
    PortId = portId;
    IsOutputPort = isOutput;
  }

  /// <summary>
  /// The canvas element that was hit.
  /// </summary>
  public ICanvasElement Element { get; }

  /// <summary>
  /// Optional handle identifier (e.g., "resize", "rotate").
  /// Null if the main element was hit.
  /// </summary>
  public string? Handle { get; }

  /// <summary>
  /// Optional port identifier for node port hits.
  /// Null if not a port hit.
  /// </summary>
  public string? PortId { get; }

  /// <summary>
  /// For port hits, indicates whether this is an output port (true) or input port (false).
  /// </summary>
  public bool IsOutputPort { get; }

  /// <summary>
  /// Convenience property: true if this is a port hit.
  /// </summary>
  public bool IsPort => PortId != null;

  /// <summary>
  /// Convenience property: true if this is a handle hit.
  /// </summary>
  public bool IsHandle => Handle != null;

  /// <summary>
  /// Convenience property: gets the element as a Node if applicable.
  /// </summary>
  public Node? AsNode => Element as Node;

  /// <summary>
  /// Convenience property: gets the element as an Edge if applicable.
  /// </summary>
  public Edge? AsEdge => Element as Edge;

  /// <summary>
  /// Convenience property: gets the element as a ShapeElement if applicable.
  /// </summary>
  public ShapeElement? AsShape => Element as ShapeElement;

  /// <summary>
  /// Creates an ElementHitInfo for a regular element hit.
  /// </summary>
  public static ElementHitInfo FromElement(ICanvasElement element) => new(element);

  /// <summary>
  /// Creates an ElementHitInfo for an element handle hit.
  /// </summary>
  public static ElementHitInfo FromHandle(ICanvasElement element, string handle) => new(element, handle);

  /// <summary>
  /// Creates an ElementHitInfo for a node port hit.
  /// </summary>
  public static ElementHitInfo FromPort(Node node, string portId, bool isOutput) => new(node, portId, isOutput);

  public override string ToString()
  {
    if (IsPort)
      return $"Port({AsNode?.Label ?? Element.Id}, {PortId}, {(IsOutputPort ? "Output" : "Input")})";
    if (IsHandle)
      return $"Handle({Element.Id}, {Handle})";
    return $"Element({Element.Id})";
  }
}
