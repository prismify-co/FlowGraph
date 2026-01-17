using Avalonia;
using FlowGraph.Core.Coordinates;
using AvaloniaPoint = Avalonia.Point;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaSize = Avalonia.Size;

namespace FlowGraph.Avalonia.Coordinates;

/// <summary>
/// Extension methods for converting between FlowGraph typed coordinates and Avalonia types.
/// 
/// <para>
/// These extension methods provide seamless integration between the type-safe coordinate
/// system and Avalonia's native types. Use these at the boundary between coordinate-safe
/// code and Avalonia-specific rendering/hit-testing code.
/// </para>
/// </summary>
/// <example>
/// <code>
/// // Get typed coordinate from input
/// CanvasPoint canvasPos = context.Coordinates.GetPointerCanvasPosition(e);
/// 
/// // Convert to Avalonia for rendering
/// context.DrawEllipse(brush, pen, canvasPos.ToAvalonia(), 5, 5);
/// 
/// // Convert from Avalonia hit test result
/// var hitPoint = e.GetPosition(mainCanvas);
/// var canvasHit = hitPoint.ToCanvasPoint();
/// </code>
/// </example>
public static class AvaloniaCoordinateExtensions
{
  #region CanvasPoint ↔ Avalonia.Point

  /// <summary>
  /// Converts a CanvasPoint to an Avalonia Point.
  /// </summary>
  public static AvaloniaPoint ToAvalonia(this CanvasPoint canvas)
      => new(canvas.X, canvas.Y);

  /// <summary>
  /// Converts an Avalonia Point to a CanvasPoint.
  /// Use when you know the point is in canvas space (e.g., from GetPosition(MainCanvas)).
  /// </summary>
  public static CanvasPoint ToCanvasPoint(this AvaloniaPoint point)
      => new(point.X, point.Y);

  #endregion

  #region ViewportPoint ↔ Avalonia.Point

  /// <summary>
  /// Converts a ViewportPoint to an Avalonia Point.
  /// </summary>
  public static AvaloniaPoint ToAvalonia(this ViewportPoint viewport)
      => new(viewport.X, viewport.Y);

  /// <summary>
  /// Converts an Avalonia Point to a ViewportPoint.
  /// Use when you know the point is in viewport space (e.g., from GetPosition(RootPanel)).
  /// </summary>
  public static ViewportPoint ToViewportPoint(this AvaloniaPoint point)
      => new(point.X, point.Y);

  #endregion

  #region CanvasVector ↔ Avalonia.Point (as delta)

  /// <summary>
  /// Converts a CanvasVector to an Avalonia Point (treating vector as point).
  /// Some Avalonia APIs use Point for both positions and deltas.
  /// </summary>
  public static AvaloniaPoint ToAvalonia(this CanvasVector vector)
      => new(vector.DX, vector.DY);

  /// <summary>
  /// Converts an Avalonia Point (used as delta) to a CanvasVector.
  /// </summary>
  public static CanvasVector ToCanvasVector(this AvaloniaPoint point)
      => new(point.X, point.Y);

  #endregion

  #region ViewportVector ↔ Avalonia.Point (as delta)

  /// <summary>
  /// Converts a ViewportVector to an Avalonia Point (treating vector as point).
  /// </summary>
  public static AvaloniaPoint ToAvalonia(this ViewportVector vector)
      => new(vector.DX, vector.DY);

  /// <summary>
  /// Converts an Avalonia Point (used as delta) to a ViewportVector.
  /// </summary>
  public static ViewportVector ToViewportVector(this AvaloniaPoint point)
      => new(point.X, point.Y);

  #endregion

  #region CanvasRect ↔ Avalonia.Rect

  /// <summary>
  /// Converts a CanvasRect to an Avalonia Rect.
  /// </summary>
  public static AvaloniaRect ToAvalonia(this CanvasRect canvas)
      => new(canvas.X, canvas.Y, canvas.Width, canvas.Height);

  /// <summary>
  /// Converts an Avalonia Rect to a CanvasRect.
  /// Use when you know the rect is in canvas space.
  /// </summary>
  public static CanvasRect ToCanvasRect(this AvaloniaRect rect)
      => new(rect.X, rect.Y, rect.Width, rect.Height);

  #endregion

  #region ViewportRect ↔ Avalonia.Rect

  /// <summary>
  /// Converts a ViewportRect to an Avalonia Rect.
  /// </summary>
  public static AvaloniaRect ToAvalonia(this ViewportRect viewport)
      => new(viewport.X, viewport.Y, viewport.Width, viewport.Height);

  /// <summary>
  /// Converts an Avalonia Rect to a ViewportRect.
  /// Use when you know the rect is in viewport space.
  /// </summary>
  public static ViewportRect ToViewportRect(this AvaloniaRect rect)
      => new(rect.X, rect.Y, rect.Width, rect.Height);

  #endregion

  #region Size Conversions

  /// <summary>
  /// Converts an Avalonia Size to a CanvasVector (treating size as dimensions).
  /// </summary>
  public static CanvasVector ToCanvasVector(this AvaloniaSize size)
      => new(size.Width, size.Height);

  /// <summary>
  /// Converts an Avalonia Size to a ViewportVector (treating size as dimensions).
  /// </summary>
  public static ViewportVector ToViewportVector(this AvaloniaSize size)
      => new(size.Width, size.Height);

  #endregion

  #region Viewport ↔ Canvas via Transformer

  /// <summary>
  /// Converts an Avalonia Point from viewport to canvas space using a transformer.
  /// </summary>
  /// <param name="viewportPoint">Point in viewport coordinates.</param>
  /// <param name="transformer">The coordinate transformer.</param>
  /// <returns>Point in canvas coordinates.</returns>
  public static AvaloniaPoint ToCanvasSpace(
      this AvaloniaPoint viewportPoint,
      ITypedCoordinateTransformer transformer)
  {
    var canvas = transformer.ToCanvas(viewportPoint.ToViewportPoint());
    return canvas.ToAvalonia();
  }

  /// <summary>
  /// Converts an Avalonia Point from canvas to viewport space using a transformer.
  /// </summary>
  /// <param name="canvasPoint">Point in canvas coordinates.</param>
  /// <param name="transformer">The coordinate transformer.</param>
  /// <returns>Point in viewport coordinates.</returns>
  public static AvaloniaPoint ToViewportSpace(
      this AvaloniaPoint canvasPoint,
      ITypedCoordinateTransformer transformer)
  {
    var viewport = transformer.ToViewport(canvasPoint.ToCanvasPoint());
    return viewport.ToAvalonia();
  }

  /// <summary>
  /// Converts an Avalonia Rect from canvas to viewport space using a transformer.
  /// </summary>
  public static AvaloniaRect ToViewportSpace(
      this AvaloniaRect canvasRect,
      ITypedCoordinateTransformer transformer)
  {
    var viewport = transformer.ToViewport(canvasRect.ToCanvasRect());
    return viewport.ToAvalonia();
  }

  /// <summary>
  /// Converts an Avalonia Rect from viewport to canvas space using a transformer.
  /// </summary>
  public static AvaloniaRect ToCanvasSpace(
      this AvaloniaRect viewportRect,
      ITypedCoordinateTransformer transformer)
  {
    var canvas = transformer.ToCanvas(viewportRect.ToViewportRect());
    return canvas.ToAvalonia();
  }

  #endregion
}
