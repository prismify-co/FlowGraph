using Avalonia.Input;
using FlowGraph.Core.Coordinates;

namespace FlowGraph.Avalonia.Input;

/// <summary>
/// Provides coordinate information for input handling in a rendering-mode agnostic way.
/// 
/// <para>
/// This interface abstracts away the differences between Visual Tree and Direct Rendering modes,
/// providing a consistent API for input states to work with coordinates. All interaction logic
/// should use this interface rather than directly accessing MainCanvas or RootPanel.
/// </para>
/// 
/// <para>
/// <b>Key principle:</b> Interaction code should work in canvas coordinates. This interface
/// ensures that pointer positions are correctly converted regardless of rendering mode.
/// </para>
/// </summary>
/// <example>
/// <code>
/// public override StateTransitionResult HandlePointerMoved(InputStateContext context, PointerEventArgs e)
/// {
///     // Get canvas position - works in both rendering modes
///     CanvasPoint canvasPos = context.Coordinates.GetPointerCanvasPosition(e);
///     
///     // Use for hit testing, snapping, etc.
///     var snapTarget = FindSnapTarget(canvasPos);
///     
///     // Get viewport position for auto-pan edge detection
///     ViewportPoint viewportPos = context.Coordinates.GetPointerViewportPosition(e);
///     if (viewportPos.IsNearEdge(context.Coordinates.GetViewportBounds(), edgeDistance))
///     {
///         // Trigger auto-pan
///     }
/// }
/// </code>
/// </example>
public interface IInputCoordinates
{
  /// <summary>
  /// Gets the current pointer position in canvas coordinates.
  /// 
  /// <para>
  /// This is the primary method for getting pointer position during interactions.
  /// Use for hit testing, node positioning, edge endpoints, selection bounds, etc.
  /// </para>
  /// 
  /// <para>
  /// Works correctly regardless of rendering mode:
  /// <list type="bullet">
  /// <item>Visual Tree mode: Uses GetPosition(MainCanvas) which applies inverse transform</item>
  /// <item>Direct Rendering mode: Uses GetPosition(RootPanel) + manual inverse transform</item>
  /// </list>
  /// </para>
  /// </summary>
  /// <param name="e">The pointer event.</param>
  /// <returns>Pointer position in canvas coordinates.</returns>
  CanvasPoint GetPointerCanvasPosition(PointerEventArgs e);

  /// <summary>
  /// Gets the current pointer position in viewport coordinates.
  /// 
  /// <para>
  /// Use for operations that work in screen space:
  /// <list type="bullet">
  /// <item>Auto-pan edge detection</item>
  /// <item>Fixed overlay positioning</item>
  /// <item>Tooltip placement</item>
  /// </list>
  /// </para>
  /// </summary>
  /// <param name="e">The pointer event.</param>
  /// <returns>Pointer position in viewport coordinates.</returns>
  ViewportPoint GetPointerViewportPosition(PointerEventArgs e);

  /// <summary>
  /// Gets the visible area in canvas coordinates.
  /// 
  /// <para>
  /// This represents the portion of the canvas that is currently visible in the viewport.
  /// Useful for:
  /// <list type="bullet">
  /// <item>Viewport culling during rendering</item>
  /// <item>Determining if a node is visible</item>
  /// <item>Calculating visible bounds for layout operations</item>
  /// </list>
  /// </para>
  /// </summary>
  CanvasRect GetVisibleCanvasRect();

  /// <summary>
  /// Gets the viewport bounds (size of the visible area).
  /// 
  /// <para>
  /// Returns a rect at (0,0) with the viewport's width and height.
  /// Use for auto-pan edge detection.
  /// </para>
  /// </summary>
  ViewportRect GetViewportBounds();

  /// <summary>
  /// Gets the current zoom level.
  /// </summary>
  double Zoom { get; }

  /// <summary>
  /// Converts a canvas point to viewport coordinates.
  /// </summary>
  ViewportPoint ToViewport(CanvasPoint canvas);

  /// <summary>
  /// Converts a viewport point to canvas coordinates.
  /// </summary>
  CanvasPoint ToCanvas(ViewportPoint viewport);

  /// <summary>
  /// Converts a canvas vector (delta) to viewport space.
  /// </summary>
  ViewportVector ToViewport(CanvasVector canvasDelta);

  /// <summary>
  /// Converts a viewport vector (delta) to canvas space.
  /// </summary>
  CanvasVector ToCanvas(ViewportVector viewportDelta);
}
