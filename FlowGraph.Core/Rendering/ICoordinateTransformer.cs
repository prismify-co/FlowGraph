using FlowGraph.Core.Elements;

namespace FlowGraph.Core.Rendering;

/// <summary>
/// Provides bidirectional coordinate transformation between canvas and screen space.
/// 
/// <para>
/// <b>Coordinate Spaces:</b>
/// <list type="bullet">
/// <item><b>Canvas Space:</b> Logical coordinates where graph elements live. 
/// Node positions (e.g., node.Position.X = 100) are in canvas space. 
/// These values are stable regardless of zoom/pan.</item>
/// <item><b>Screen Space:</b> Pixel coordinates on the display.
/// Pointer events (e.g., e.GetPosition(control)) return screen space.
/// These values change as the user zooms and pans.</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Transform Formulas:</b>
/// <code>
/// ScreenToCanvas: canvasPoint = (screenPoint - offset) / zoom
/// CanvasToScreen: screenPoint = canvasPoint * zoom + offset
/// </code>
/// </para>
/// 
/// <para>
/// <b>Inspired by:</b>
/// <list type="bullet">
/// <item>react-diagrams: CanvasEngine.getRelativeMousePoint(), getRelativePoint()</item>
/// <item>Konva.js: Stage.getPointerPosition() vs Node.getRelativePointerPosition()</item>
/// <item>AnyChart: scale.transform() / scale.inverseTransform()</item>
/// </list>
/// </para>
/// </summary>
public interface ICoordinateTransformer
{
    /// <summary>
    /// The current zoom level (1.0 = 100%, 2.0 = 200%, 0.5 = 50%).
    /// </summary>
    double Zoom { get; }
    
    /// <summary>
    /// The X component of the current pan offset in screen coordinates.
    /// </summary>
    double OffsetX { get; }
    
    /// <summary>
    /// The Y component of the current pan offset in screen coordinates.
    /// </summary>
    double OffsetY { get; }
    
    /// <summary>
    /// Transforms a point from screen space to canvas space.
    /// 
    /// <para><b>When to use:</b></para>
    /// <list type="bullet">
    /// <item>Converting pointer event positions to canvas coordinates for hit testing</item>
    /// <item>Determining which graph elements are under the cursor</item>
    /// <item>Translating drag deltas to canvas movement (use <see cref="ScreenToCanvasDelta"/> for deltas)</item>
    /// </list>
    /// 
    /// <para><b>Formula:</b> <c>canvasPoint = (screenPoint - offset) / zoom</c></para>
    /// </summary>
    /// <param name="screenX">X coordinate in screen space.</param>
    /// <param name="screenY">Y coordinate in screen space.</param>
    /// <returns>The equivalent point in canvas coordinates.</returns>
    [return: CoordinateSpace(CoordinateSpace.Canvas)]
    Point ScreenToCanvas(
        [CoordinateSpace(CoordinateSpace.Screen)] double screenX, 
        [CoordinateSpace(CoordinateSpace.Screen)] double screenY);
    
    /// <summary>
    /// Transforms a point from canvas space to screen space.
    /// 
    /// <para><b>When to use:</b></para>
    /// <list type="bullet">
    /// <item>Drawing directly to a DrawingContext (bypassing visual tree transforms)</item>
    /// <item>Positioning overlay UI elements that should appear at canvas locations</item>
    /// <item>Calculating visible bounds in screen space for culling</item>
    /// </list>
    /// 
    /// <para><b>When NOT to use:</b></para>
    /// <list type="bullet">
    /// <item>Positioning elements with Canvas.SetLeft/SetTop inside a transformed container 
    /// - those should use canvas coordinates directly</item>
    /// </list>
    /// 
    /// <para><b>Formula:</b> <c>screenPoint = canvasPoint * zoom + offset</c></para>
    /// </summary>
    /// <param name="canvasX">X coordinate in canvas space.</param>
    /// <param name="canvasY">Y coordinate in canvas space.</param>
    /// <returns>The equivalent point in screen coordinates.</returns>
    [return: CoordinateSpace(CoordinateSpace.Screen)]
    Point CanvasToScreen(
        [CoordinateSpace(CoordinateSpace.Canvas)] double canvasX, 
        [CoordinateSpace(CoordinateSpace.Canvas)] double canvasY);
    
    /// <summary>
    /// Transforms a delta/vector from screen space to canvas space.
    /// Unlike point transforms, this only applies zoom (not offset).
    /// 
    /// <para><b>When to use:</b></para>
    /// <list type="bullet">
    /// <item>Converting drag distances to canvas movement amounts</item>
    /// <item>Zoom-adjusted displacement calculations</item>
    /// <item>Measuring screen distances in canvas units</item>
    /// </list>
    /// 
    /// <para><b>Formula:</b> <c>canvasDelta = screenDelta / zoom</c></para>
    /// </summary>
    /// <param name="screenDeltaX">X delta in screen space.</param>
    /// <param name="screenDeltaY">Y delta in screen space.</param>
    /// <returns>The equivalent delta in canvas coordinates.</returns>
    Point ScreenToCanvasDelta(double screenDeltaX, double screenDeltaY);
    
    /// <summary>
    /// Transforms a delta/vector from canvas space to screen space.
    /// Unlike point transforms, this only applies zoom (not offset).
    /// 
    /// <para><b>Formula:</b> <c>screenDelta = canvasDelta * zoom</c></para>
    /// </summary>
    /// <param name="canvasDeltaX">X delta in canvas space.</param>
    /// <param name="canvasDeltaY">Y delta in canvas space.</param>
    /// <returns>The equivalent delta in screen coordinates.</returns>
    Point CanvasToScreenDelta(double canvasDeltaX, double canvasDeltaY);
}

/// <summary>
/// Extension methods for <see cref="ICoordinateTransformer"/>.
/// </summary>
public static class CoordinateTransformerExtensions
{
    /// <summary>
    /// Transforms a point from screen space to canvas space.
    /// </summary>
    [return: CoordinateSpace(CoordinateSpace.Canvas)]
    public static Point ScreenToCanvas(
        this ICoordinateTransformer transformer, 
        [CoordinateSpace(CoordinateSpace.Screen)] Point screenPoint)
    {
        return transformer.ScreenToCanvas(screenPoint.X, screenPoint.Y);
    }
    
    /// <summary>
    /// Transforms a point from canvas space to screen space.
    /// </summary>
    [return: CoordinateSpace(CoordinateSpace.Screen)]
    public static Point CanvasToScreen(
        this ICoordinateTransformer transformer, 
        [CoordinateSpace(CoordinateSpace.Canvas)] Point canvasPoint)
    {
        return transformer.CanvasToScreen(canvasPoint.X, canvasPoint.Y);
    }
    
    /// <summary>
    /// Transforms a rectangle from canvas space to screen space.
    /// </summary>
    [return: CoordinateSpace(CoordinateSpace.Screen)]
    public static Rect CanvasToScreen(
        this ICoordinateTransformer transformer, 
        [CoordinateSpace(CoordinateSpace.Canvas)] Rect canvasRect)
    {
        var topLeft = transformer.CanvasToScreen(canvasRect.X, canvasRect.Y);
        var size = transformer.CanvasToScreenDelta(canvasRect.Width, canvasRect.Height);
        return new Rect(topLeft.X, topLeft.Y, size.X, size.Y);
    }
    
    /// <summary>
    /// Transforms a rectangle from screen space to canvas space.
    /// </summary>
    [return: CoordinateSpace(CoordinateSpace.Canvas)]
    public static Rect ScreenToCanvas(
        this ICoordinateTransformer transformer, 
        [CoordinateSpace(CoordinateSpace.Screen)] Rect screenRect)
    {
        var topLeft = transformer.ScreenToCanvas(screenRect.X, screenRect.Y);
        var size = transformer.ScreenToCanvasDelta(screenRect.Width, screenRect.Height);
        return new Rect(topLeft.X, topLeft.Y, size.X, size.Y);
    }
}
