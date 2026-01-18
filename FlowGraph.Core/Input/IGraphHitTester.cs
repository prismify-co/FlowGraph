using FlowGraph.Core.Elements;
using FlowGraph.Core.Rendering;

namespace FlowGraph.Core.Input;

/// <summary>
/// The type of element hit during a graph hit test.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="FlagsAttribute"/> to enable efficient bitwise operations
/// for processors that handle multiple element types. This allows O(1) type
/// checking instead of list iteration on high-frequency events like PointerMoved.
/// </para>
/// <para>
/// Example: A processor handling both Node and Group can use:
/// <code>HitTargetTypes.Node | HitTargetTypes.Group</code>
/// </para>
/// </remarks>
[Flags]
public enum HitTargetType
{
    /// <summary>Nothing was hit (empty canvas space).</summary>
    None = 0,
    
    /// <summary>Empty canvas space was hit.</summary>
    Canvas = 1 << 0,
    
    /// <summary>A node was hit.</summary>
    Node = 1 << 1,
    
    /// <summary>An edge/connection was hit.</summary>
    Edge = 1 << 2,
    
    /// <summary>A port on a node was hit.</summary>
    Port = 1 << 3,
    
    /// <summary>A resize handle was hit.</summary>
    ResizeHandle = 1 << 4,
    
    /// <summary>A group/container was hit.</summary>
    Group = 1 << 5,
    
    /// <summary>A waypoint on an edge was hit.</summary>
    Waypoint = 1 << 6,
    
    /// <summary>A shape (non-node visual element) was hit.</summary>
    Shape = 1 << 7,
    
    /// <summary>A custom element type was hit.</summary>
    Custom = 1 << 8,
    
    // Common combinations for processor registration
    
    /// <summary>All element types (for catch-all processors).</summary>
    All = Canvas | Node | Edge | Port | ResizeHandle | Group | Waypoint | Shape | Custom,
    
    /// <summary>All draggable elements (nodes, shapes, groups).</summary>
    Draggable = Node | Shape | Group,
    
    /// <summary>All selectable elements.</summary>
    Selectable = Node | Edge | Shape | Group
}

/// <summary>
/// Result of a graph-wide hit test, identifying what element (if any) was hit
/// and providing typed access to the target.
/// </summary>
public class GraphHitTestResult
{
    /// <summary>
    /// The type of element that was hit.
    /// </summary>
    public HitTargetType TargetType { get; init; } = HitTargetType.None;
    
    /// <summary>
    /// The raw target object that was hit.
    /// Use typed accessors (<see cref="Node"/>, <see cref="Edge"/>, etc.) for convenience.
    /// </summary>
    public object? Target { get; init; }
    
    /// <summary>
    /// The position where the hit occurred, in canvas coordinates.
    /// </summary>
    [CoordinateSpace(CoordinateSpace.Canvas)]
    public Point CanvasPosition { get; init; }
    
    /// <summary>
    /// Distance from the exact hit point to the element.
    /// Zero for direct hits, positive for tolerance-based hits.
    /// </summary>
    public double Distance { get; init; }
    
    /// <summary>
    /// Whether anything was hit (excludes <see cref="HitTargetType.None"/> and <see cref="HitTargetType.Canvas"/>).
    /// </summary>
    public bool IsHit => TargetType != HitTargetType.None && TargetType != HitTargetType.Canvas;
    
    /// <summary>
    /// Whether empty canvas space was clicked (no element hit).
    /// </summary>
    public bool IsCanvasHit => TargetType == HitTargetType.Canvas || TargetType == HitTargetType.None;
    
    // Convenience typed accessors
    
    /// <summary>Gets the hit node, or null if a node was not hit.</summary>
    public Node? Node => TargetType == HitTargetType.Node ? Target as Node : null;
    
    /// <summary>Gets the hit edge, or null if an edge was not hit.</summary>
    public Edge? Edge => TargetType == HitTargetType.Edge ? Target as Edge : null;
    
    /// <summary>Gets the hit port, or null if a port was not hit.</summary>
    public Port? Port => TargetType == HitTargetType.Port && Target is PortHitInfo info ? info.Port : null;
    
    /// <summary>Gets the owner node of a hit port, or null if a port was not hit.</summary>
    public Node? PortOwner => TargetType == HitTargetType.Port && Target is PortHitInfo info ? info.Node : null;
    
    /// <summary>Gets whether the hit port is an input port.</summary>
    public bool IsInputPort => TargetType == HitTargetType.Port && Target is PortHitInfo info && info.IsInput;
    
    /// <summary>Gets the hit resize handle position, or null if a resize handle was not hit.</summary>
    public ResizeHandlePosition? ResizeHandle => TargetType == HitTargetType.ResizeHandle && Target is ResizeHandleHitInfo info 
        ? info.HandlePosition 
        : null;
    
    /// <summary>Gets the node that owns a hit resize handle, or null if a resize handle was not hit.</summary>
    public Node? ResizeHandleOwner => TargetType == HitTargetType.ResizeHandle && Target is ResizeHandleHitInfo info 
        ? info.Node 
        : null;
    
    /// <summary>
    /// Creates a result indicating empty canvas was hit.
    /// </summary>
    public static GraphHitTestResult CanvasHit(Point canvasPosition) => new()
    {
        TargetType = HitTargetType.Canvas,
        Target = null,
        CanvasPosition = canvasPosition,
        Distance = 0
    };
    
    /// <summary>
    /// Creates a result indicating a node was hit.
    /// </summary>
    public static GraphHitTestResult NodeHit(Node node, Point canvasPosition, double distance = 0) => new()
    {
        TargetType = HitTargetType.Node,
        Target = node,
        CanvasPosition = canvasPosition,
        Distance = distance
    };
    
    /// <summary>
    /// Creates a result indicating an edge was hit.
    /// </summary>
    public static GraphHitTestResult EdgeHit(Edge edge, Point canvasPosition, double distance = 0) => new()
    {
        TargetType = HitTargetType.Edge,
        Target = edge,
        CanvasPosition = canvasPosition,
        Distance = distance
    };
    
    /// <summary>
    /// Creates a result indicating a port was hit.
    /// </summary>
    public static GraphHitTestResult PortHit(Node node, Port port, bool isInput, Point canvasPosition, double distance = 0) => new()
    {
        TargetType = HitTargetType.Port,
        Target = new PortHitInfo(node, port, isInput),
        CanvasPosition = canvasPosition,
        Distance = distance
    };
    
    /// <summary>
    /// Creates a result indicating a resize handle was hit.
    /// </summary>
    public static GraphHitTestResult ResizeHandleHit(Node node, ResizeHandlePosition handlePosition, Point canvasPosition) => new()
    {
        TargetType = HitTargetType.ResizeHandle,
        Target = new ResizeHandleHitInfo(node, handlePosition),
        CanvasPosition = canvasPosition,
        Distance = 0
    };
}

/// <summary>
/// Information about a hit port.
/// </summary>
public record PortHitInfo(Node Node, Port Port, bool IsInput);

/// <summary>
/// Information about a hit resize handle.
/// </summary>
public record ResizeHandleHitInfo(Node Node, ResizeHandlePosition HandlePosition);

/// <summary>
/// Positions of resize handles on a node.
/// </summary>
public enum ResizeHandlePosition
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

/// <summary>
/// Hit tester for the entire graph surface.
/// Aggregates hit tests across all element types with proper priority ordering.
/// 
/// <para>
/// <b>Hit Test Priority Order (first match wins):</b>
/// <list type="number">
/// <item>Resize handles (highest priority - always on top)</item>
/// <item>Ports (small targets need priority)</item>
/// <item>Nodes (front to back by Z-order)</item>
/// <item>Edges (may need tolerance-based hit testing)</item>
/// <item>Groups (behind their contents)</item>
/// <item>Canvas (empty space, lowest priority)</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Inspired by:</b>
/// <list type="bullet">
/// <item>react-diagrams: DiagramEngine.getModelAtPosition() with type filtering</item>
/// <item>Konva.js: Stage.getIntersection() returning topmost shape</item>
/// <item>draw.io/mxGraph: mxGraph.getCellAt() with priority handling</item>
/// </list>
/// </para>
/// </summary>
public interface IGraphHitTester
{
    /// <summary>
    /// The coordinate space expected for hit test inputs.
    /// 
    /// <para>
    /// Standard implementations expect <see cref="CoordinateSpace.Canvas"/> coordinates.
    /// Callers using screen coordinates (e.g., from pointer events) must convert
    /// using <see cref="ICoordinateTransformer.ScreenToCanvas"/> first.
    /// </para>
    /// </summary>
    CoordinateSpace InputCoordinateSpace { get; }
    
    /// <summary>
    /// Performs a comprehensive hit test against all graph elements.
    /// </summary>
    /// <param name="position">
    /// Position to test in the coordinate space specified by 
    /// <see cref="InputCoordinateSpace"/>.
    /// </param>
    /// <returns>
    /// The hit test result identifying what was hit. If nothing specific was hit,
    /// returns a result with <see cref="HitTargetType.Canvas"/>.
    /// </returns>
    GraphHitTestResult HitTest(Point position);
    
    /// <summary>
    /// Performs a hit test with custom tolerance values.
    /// </summary>
    /// <param name="position">Position to test.</param>
    /// <param name="nodeTolerance">Extra tolerance for node bounds.</param>
    /// <param name="edgeTolerance">Extra tolerance for edge paths.</param>
    /// <param name="portTolerance">Extra tolerance for port targets.</param>
    /// <returns>The hit test result.</returns>
    GraphHitTestResult HitTest(
        Point position, 
        double nodeTolerance = 0, 
        double edgeTolerance = 5, 
        double portTolerance = 8);
    
    /// <summary>
    /// Performs a hit test filtering to specific element types.
    /// </summary>
    /// <param name="position">Position to test.</param>
    /// <param name="targetTypes">Which element types to consider.</param>
    /// <returns>The hit test result, only matching specified types.</returns>
    GraphHitTestResult HitTest(Point position, params HitTargetType[] targetTypes);
}
