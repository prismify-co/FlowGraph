using FlowGraph.Core.Elements;

namespace FlowGraph.Core.Rendering;

/// <summary>
/// Provides bidirectional coordinate transformation between canvas and viewport space.
/// 
/// <para>
/// <b>Coordinate Spaces:</b>
/// <list type="bullet">
/// <item><b>Canvas Space:</b> Logical coordinates where graph elements live. 
/// Node positions (e.g., node.Position.X = 100) are in canvas space. 
/// These values are stable regardless of zoom/pan.</item>
/// <item><b>Viewport Space:</b> Coordinates within the viewport window (the visible area).
/// The viewport origin (0,0) is the top-left corner of the visible canvas area.
/// These coordinates change as the user zooms and pans.</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>IMPORTANT - Viewport vs Control Coordinates:</b>
/// <list type="bullet">
/// <item>Viewport coordinates assume (0,0) is at the viewport's visual origin.</item>
/// <item>Control coordinates (e.g., from <c>e.GetPosition(RootPanel)</c>) may have an offset
/// if the canvas is not at (0,0) within the control (e.g., due to toolbars, margins).</item>
/// <item>For pointer events, prefer <c>e.GetPosition(MainCanvas)</c> which gives canvas coords directly,
/// or use the viewport position if you need to account for the transform manually.</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Transform Formulas:</b>
/// <code>
/// ViewportToCanvas: canvasPoint = (viewportPoint - offset) / zoom
/// CanvasToViewport: viewportPoint = canvasPoint * zoom + offset
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
    /// The X component of the current pan offset in viewport coordinates.
    /// </summary>
    double OffsetX { get; }

    /// <summary>
    /// The Y component of the current pan offset in viewport coordinates.
    /// </summary>
    double OffsetY { get; }

    /// <summary>
    /// Transforms a point from viewport space to canvas space.
    /// 
    /// <para><b>When to use:</b></para>
    /// <list type="bullet">
    /// <item>Converting viewport-relative positions to canvas coordinates</item>
    /// <item>Inverse calculations from CanvasToViewport operations</item>
    /// </list>
    /// 
    /// <para><b>When NOT to use:</b></para>
    /// <list type="bullet">
    /// <item>For pointer events - prefer <c>e.GetPosition(MainCanvas)</c> which gives canvas coords directly</item>
    /// <item>When the input is from RootPanel and canvas has an offset within RootPanel</item>
    /// </list>
    /// 
    /// <para><b>Formula:</b> <c>canvasPoint = (viewportPoint - offset) / zoom</c></para>
    /// </summary>
    /// <param name="viewportX">X coordinate in viewport space.</param>
    /// <param name="viewportY">Y coordinate in viewport space.</param>
    /// <returns>The equivalent point in canvas coordinates.</returns>
    [return: CoordinateSpace(CoordinateSpace.Canvas)]
    Point ViewportToCanvas(
        [CoordinateSpace(CoordinateSpace.Viewport)] double viewportX,
        [CoordinateSpace(CoordinateSpace.Viewport)] double viewportY);

    /// <summary>
    /// Transforms a point from canvas space to viewport space.
    /// 
    /// <para><b>When to use:</b></para>
    /// <list type="bullet">
    /// <item>Drawing directly to a DrawingContext (bypassing visual tree transforms)</item>
    /// <item>Positioning overlay UI elements relative to the viewport</item>
    /// <item>Calculating visible bounds in viewport space for culling</item>
    /// </list>
    /// 
    /// <para><b>When NOT to use:</b></para>
    /// <list type="bullet">
    /// <item>Positioning elements with Canvas.SetLeft/SetTop inside a transformed container 
    /// - those should use canvas coordinates directly</item>
    /// </list>
    /// 
    /// <para><b>Formula:</b> <c>viewportPoint = canvasPoint * zoom + offset</c></para>
    /// </summary>
    /// <param name="canvasX">X coordinate in canvas space.</param>
    /// <param name="canvasY">Y coordinate in canvas space.</param>
    /// <returns>The equivalent point in viewport coordinates.</returns>
    [return: CoordinateSpace(CoordinateSpace.Viewport)]
    Point CanvasToViewport(
        [CoordinateSpace(CoordinateSpace.Canvas)] double canvasX,
        [CoordinateSpace(CoordinateSpace.Canvas)] double canvasY);

    /// <summary>
    /// Transforms a delta/vector from viewport space to canvas space.
    /// Unlike point transforms, this only applies zoom (not offset).
    /// 
    /// <para><b>When to use:</b></para>
    /// <list type="bullet">
    /// <item>Converting drag distances to canvas movement amounts</item>
    /// <item>Zoom-adjusted displacement calculations</item>
    /// <item>Measuring viewport distances in canvas units</item>
    /// </list>
    /// 
    /// <para><b>Formula:</b> <c>canvasDelta = viewportDelta / zoom</c></para>
    /// </summary>
    /// <param name="viewportDeltaX">X delta in viewport space.</param>
    /// <param name="viewportDeltaY">Y delta in viewport space.</param>
    /// <returns>The equivalent delta in canvas coordinates.</returns>
    Point ViewportToCanvasDelta(double viewportDeltaX, double viewportDeltaY);

    /// <summary>
    /// Transforms a delta/vector from canvas space to viewport space.
    /// Unlike point transforms, this only applies zoom (not offset).
    /// 
    /// <para><b>Formula:</b> <c>viewportDelta = canvasDelta * zoom</c></para>
    /// </summary>
    /// <param name="canvasDeltaX">X delta in canvas space.</param>
    /// <param name="canvasDeltaY">Y delta in canvas space.</param>
    /// <returns>The equivalent delta in viewport coordinates.</returns>
    Point CanvasToViewportDelta(double canvasDeltaX, double canvasDeltaY);

    #region Obsolete methods for backward compatibility

    /// <summary>
    /// Transforms a point from screen space to canvas space.
    /// </summary>
    /// <remarks>
    /// <b>DEPRECATED:</b> The term "Screen" is ambiguous. Use <see cref="ViewportToCanvas"/> instead,
    /// or better yet, use <c>e.GetPosition(MainCanvas)</c> for pointer events which gives canvas coords directly.
    /// </remarks>
    [Obsolete("Use ViewportToCanvas instead. 'Screen' terminology was ambiguous - see ICoordinateTransformer docs.")]
    [return: CoordinateSpace(CoordinateSpace.Canvas)]
    Point ScreenToCanvas(
        [CoordinateSpace(CoordinateSpace.Viewport)] double screenX,
        [CoordinateSpace(CoordinateSpace.Viewport)] double screenY)
        => ViewportToCanvas(screenX, screenY);

    /// <summary>
    /// Transforms a point from canvas space to screen space.
    /// </summary>
    /// <remarks>
    /// <b>DEPRECATED:</b> The term "Screen" is ambiguous. Use <see cref="CanvasToViewport"/> instead.
    /// </remarks>
    [Obsolete("Use CanvasToViewport instead. 'Screen' terminology was ambiguous - see ICoordinateTransformer docs.")]
    [return: CoordinateSpace(CoordinateSpace.Viewport)]
    Point CanvasToScreen(
        [CoordinateSpace(CoordinateSpace.Canvas)] double canvasX,
        [CoordinateSpace(CoordinateSpace.Canvas)] double canvasY)
        => CanvasToViewport(canvasX, canvasY);

    /// <summary>
    /// Transforms a delta/vector from screen space to canvas space.
    /// </summary>
    [Obsolete("Use ViewportToCanvasDelta instead. 'Screen' terminology was ambiguous.")]
    Point ScreenToCanvasDelta(double screenDeltaX, double screenDeltaY)
        => ViewportToCanvasDelta(screenDeltaX, screenDeltaY);

    /// <summary>
    /// Transforms a delta/vector from canvas space to screen space.
    /// </summary>
    [Obsolete("Use CanvasToViewportDelta instead. 'Screen' terminology was ambiguous.")]
    Point CanvasToScreenDelta(double canvasDeltaX, double canvasDeltaY)
        => CanvasToViewportDelta(canvasDeltaX, canvasDeltaY);

    #endregion
}

/// <summary>
/// Extension methods for <see cref="ICoordinateTransformer"/>.
/// </summary>
public static class CoordinateTransformerExtensions
{
    /// <summary>
    /// Transforms a point from viewport space to canvas space.
    /// </summary>
    [return: CoordinateSpace(CoordinateSpace.Canvas)]
    public static Point ViewportToCanvas(
        this ICoordinateTransformer transformer,
        [CoordinateSpace(CoordinateSpace.Viewport)] Point viewportPoint)
    {
        return transformer.ViewportToCanvas(viewportPoint.X, viewportPoint.Y);
    }

    /// <summary>
    /// Transforms a point from canvas space to viewport space.
    /// </summary>
    [return: CoordinateSpace(CoordinateSpace.Viewport)]
    public static Point CanvasToViewport(
        this ICoordinateTransformer transformer,
        [CoordinateSpace(CoordinateSpace.Canvas)] Point canvasPoint)
    {
        return transformer.CanvasToViewport(canvasPoint.X, canvasPoint.Y);
    }

    /// <summary>
    /// Transforms a rectangle from canvas space to viewport space.
    /// </summary>
    [return: CoordinateSpace(CoordinateSpace.Viewport)]
    public static Rect CanvasToViewport(
        this ICoordinateTransformer transformer,
        [CoordinateSpace(CoordinateSpace.Canvas)] Rect canvasRect)
    {
        var topLeft = transformer.CanvasToViewport(canvasRect.X, canvasRect.Y);
        var size = transformer.CanvasToViewportDelta(canvasRect.Width, canvasRect.Height);
        return new Rect(topLeft.X, topLeft.Y, size.X, size.Y);
    }

    /// <summary>
    /// Transforms a rectangle from viewport space to canvas space.
    /// </summary>
    [return: CoordinateSpace(CoordinateSpace.Canvas)]
    public static Rect ViewportToCanvas(
        this ICoordinateTransformer transformer,
        [CoordinateSpace(CoordinateSpace.Viewport)] Rect viewportRect)
    {
        var topLeft = transformer.ViewportToCanvas(viewportRect.X, viewportRect.Y);
        var size = transformer.ViewportToCanvasDelta(viewportRect.Width, viewportRect.Height);
        return new Rect(topLeft.X, topLeft.Y, size.X, size.Y);
    }

    #region Obsolete extension methods

    /// <summary>
    /// Transforms a point from screen space to canvas space.
    /// </summary>
    [Obsolete("Use ViewportToCanvas instead.")]
    [return: CoordinateSpace(CoordinateSpace.Canvas)]
    public static Point ScreenToCanvas(
        this ICoordinateTransformer transformer,
        Point screenPoint)
    {
        return transformer.ViewportToCanvas(screenPoint.X, screenPoint.Y);
    }

    /// <summary>
    /// Transforms a point from canvas space to screen space.
    /// </summary>
    [Obsolete("Use CanvasToViewport instead.")]
    public static Point CanvasToScreen(
        this ICoordinateTransformer transformer,
        Point canvasPoint)
    {
        return transformer.CanvasToViewport(canvasPoint.X, canvasPoint.Y);
    }

    /// <summary>
    /// Transforms a rectangle from canvas space to screen space.
    /// </summary>
    [Obsolete("Use CanvasToViewport instead.")]
    public static Rect CanvasToScreen(
        this ICoordinateTransformer transformer,
        Rect canvasRect)
    {
        return transformer.CanvasToViewport(canvasRect);
    }

    /// <summary>
    /// Transforms a rectangle from screen space to canvas space.
    /// </summary>
    [Obsolete("Use ViewportToCanvas instead.")]
    public static Rect ScreenToCanvas(
        this ICoordinateTransformer transformer,
        Rect screenRect)
    {
        return transformer.ViewportToCanvas(screenRect);
    }

    #endregion
}
