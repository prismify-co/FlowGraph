using FlowGraph.Core.Elements;
using FlowGraph.Core.Rendering;

namespace FlowGraph.Core.Input;

/// <summary>
/// Result of a hit test operation against a specific element type.
/// </summary>
/// <typeparam name="TElement">The type of element that can be hit.</typeparam>
public class HitTestResult<TElement>
{
    /// <summary>
    /// The element that was hit, or null/default if nothing was hit.
    /// </summary>
    public TElement? Element { get; init; }
    
    /// <summary>
    /// The position where the hit occurred, in canvas coordinates.
    /// </summary>
    [CoordinateSpace(CoordinateSpace.Canvas)]
    public Point CanvasPosition { get; init; }
    
    /// <summary>
    /// The position where the hit occurred, relative to the element's origin.
    /// Only meaningful when <see cref="Element"/> is not null.
    /// </summary>
    [CoordinateSpace(CoordinateSpace.Local)]
    public Point LocalPosition { get; init; }
    
    /// <summary>
    /// Distance from the exact hit point to the element's bounds or center.
    /// Useful for edge hit testing where some tolerance is needed.
    /// </summary>
    public double Distance { get; init; }
    
    /// <summary>
    /// Whether an element was hit.
    /// </summary>
    public bool IsHit => Element is not null;
    
    /// <summary>
    /// Creates an empty (no hit) result.
    /// </summary>
    public static HitTestResult<TElement> NoHit(Point canvasPosition) => new()
    {
        Element = default,
        CanvasPosition = canvasPosition,
        Distance = double.MaxValue
    };
}

/// <summary>
/// Interface for components that support hit testing.
/// 
/// <para>
/// <b>Contract:</b> Implementations MUST document and enforce the coordinate space
/// specified by <see cref="HitTestCoordinateSpace"/>. Callers MUST provide positions
/// in the declared coordinate space.
/// </para>
/// 
/// <para>
/// <b>Inspired by:</b>
/// <list type="bullet">
/// <item>Konva.js: Stage.getIntersection(pos) returning the topmost Shape</item>
/// <item>react-diagrams: DiagramEngine.getModelAtPosition()</item>
/// <item>WPF/Avalonia: VisualTreeHelper.HitTest() pattern</item>
/// </list>
/// </para>
/// </summary>
/// <typeparam name="TElement">The type of element this tests against.</typeparam>
public interface IHitTestable<TElement>
{
    /// <summary>
    /// The coordinate space expected for the hit test position parameter.
    /// 
    /// <para><b>Implementation guidance:</b></para>
    /// <list type="bullet">
    /// <item>Most hit testers should expect <see cref="CoordinateSpace.Canvas"/> coordinates</item>
    /// <item>Direct renderers that manage their own transforms may expect <see cref="CoordinateSpace.Screen"/></item>
    /// <item>The caller is responsible for converting coordinates before calling <see cref="HitTest"/></item>
    /// </list>
    /// </summary>
    CoordinateSpace HitTestCoordinateSpace { get; }
    
    /// <summary>
    /// Performs a hit test at the specified position.
    /// </summary>
    /// <param name="position">
    /// Position to test. MUST be in the coordinate space specified by 
    /// <see cref="HitTestCoordinateSpace"/>. Passing coordinates in the wrong 
    /// space will produce incorrect results.
    /// </param>
    /// <param name="tolerance">
    /// Optional tolerance/margin for the hit test, in the same coordinate space.
    /// Use this for edge hit testing or touch-friendly targets.
    /// </param>
    /// <returns>The hit test result containing the hit element (if any) and positions.</returns>
    HitTestResult<TElement> HitTest(Point position, double tolerance = 0);
}
