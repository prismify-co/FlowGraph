using FlowGraph.Core.Elements;

namespace FlowGraph.Core.Rendering;

/// <summary>
/// Read-only viewport state for observing pan/zoom changes.
/// 
/// <para>
/// The viewport defines the visible area of the infinite canvas and provides
/// coordinate transformation between canvas space (where elements live) and
/// screen space (what the user sees).
/// </para>
/// 
/// <para>
/// <b>Inspired by:</b>
/// <list type="bullet">
/// <item>react-diagrams: CanvasModel with offsetX, offsetY, zoom</item>
/// <item>Konva.js: Stage with scale, position properties</item>
/// <item>mxGraph: mxGraphView with scale and translate</item>
/// </list>
/// </para>
/// </summary>
public interface IReadOnlyViewportState : ICoordinateTransformer
{
    /// <summary>
    /// The visible size of the viewport in screen coordinates (pixels).
    /// </summary>
    Size ViewSize { get; }
    
    /// <summary>
    /// Gets the currently visible area in canvas coordinates.
    /// 
    /// <para>
    /// This is the inverse transform of the screen bounds. Use this to determine
    /// which graph elements are potentially visible for culling/virtualization.
    /// </para>
    /// </summary>
    /// <returns>
    /// A rectangle in canvas coordinates representing the visible area.
    /// Returns <see cref="Rect.Empty"/> if <see cref="ViewSize"/> is not set.
    /// </returns>
    [return: CoordinateSpace(CoordinateSpace.Canvas)]
    Rect GetVisibleCanvasRect();
    
    /// <summary>
    /// Event raised when any viewport property changes (zoom, offset, or view size).
    /// 
    /// <para>
    /// Subscribers should invalidate their display or recalculate visibility
    /// when this event fires.
    /// </para>
    /// </summary>
    event EventHandler? ViewportChanged;
}

/// <summary>
/// Mutable viewport state for controlling pan/zoom operations.
/// 
/// <para>
/// Use this interface to programmatically control the viewport, such as
/// centering on elements, fitting to bounds, or implementing custom navigation.
/// </para>
/// </summary>
public interface IViewportState : IReadOnlyViewportState
{
    /// <summary>
    /// Sets the zoom level, optionally zooming toward a specific screen point.
    /// 
    /// <para>
    /// When <paramref name="zoomCenter"/> is provided, the canvas point under that
    /// screen position will remain stationary after zooming. This creates a natural
    /// "zoom toward cursor" behavior.
    /// </para>
    /// </summary>
    /// <param name="zoom">
    /// The new zoom level. Values are typically clamped to a min/max range
    /// (e.g., 0.1 to 5.0).
    /// </param>
    /// <param name="zoomCenter">
    /// Optional screen coordinate to zoom toward. If null, zooms toward the 
    /// center of the viewport.
    /// </param>
    void SetZoom(double zoom, [CoordinateSpace(CoordinateSpace.Screen)] Point? zoomCenter = null);
    
    /// <summary>
    /// Pans the viewport by the specified delta in screen coordinates.
    /// 
    /// <para>
    /// Positive deltaX moves the view right (canvas moves left).
    /// Positive deltaY moves the view down (canvas moves up).
    /// </para>
    /// </summary>
    /// <param name="deltaX">Pan amount in X direction (screen pixels).</param>
    /// <param name="deltaY">Pan amount in Y direction (screen pixels).</param>
    void Pan(double deltaX, double deltaY);
    
    /// <summary>
    /// Sets the pan offset directly.
    /// </summary>
    /// <param name="offsetX">New X offset in screen coordinates.</param>
    /// <param name="offsetY">New Y offset in screen coordinates.</param>
    void SetOffset(double offsetX, double offsetY);
    
    /// <summary>
    /// Centers the viewport on a specific canvas point.
    /// 
    /// <para>
    /// After this call, the specified canvas point will be at the center of the viewport.
    /// Zoom level is not changed.
    /// </para>
    /// </summary>
    /// <param name="canvasPoint">The point in canvas coordinates to center on.</param>
    void CenterOn([CoordinateSpace(CoordinateSpace.Canvas)] Point canvasPoint);
    
    /// <summary>
    /// Adjusts zoom and pan to fit the specified canvas bounds within the viewport.
    /// 
    /// <para>
    /// Use this to implement "fit to content" or "zoom to selection" features.
    /// The resulting view will show all of <paramref name="canvasBounds"/> with
    /// the specified padding around the edges.
    /// </para>
    /// </summary>
    /// <param name="canvasBounds">
    /// The bounds to fit, in canvas coordinates. Pass the bounding box of 
    /// all elements you want to show.
    /// </param>
    /// <param name="padding">
    /// Padding in screen pixels to leave around the bounds. Default is 50.
    /// </param>
    void FitToBounds([CoordinateSpace(CoordinateSpace.Canvas)] Rect canvasBounds, double padding = 50);
    
    /// <summary>
    /// Resets the viewport to default state (zoom = 1.0, centered on origin).
    /// </summary>
    void Reset();
}
