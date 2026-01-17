namespace FlowGraph.Core.Coordinates;

/// <summary>
/// Type-safe coordinate transformer that converts between canvas and viewport coordinate spaces.
/// 
/// <para>
/// This interface provides compile-time type safety for coordinate conversions by using
/// distinct types for canvas and viewport coordinates. This prevents accidental mixing
/// of coordinate spaces, which is a common source of bugs in pan/zoom implementations.
/// </para>
/// 
/// <para>
/// <b>Coordinate Space Relationships:</b>
/// <code>
/// Viewport → Canvas:  canvas = (viewport - offset) / zoom
/// Canvas → Viewport:  viewport = canvas * zoom + offset
/// </code>
/// </para>
/// </summary>
/// <example>
/// <code>
/// // Type-safe - compiler prevents passing viewport where canvas expected
/// ViewportPoint viewportPos = coordinates.GetPointerViewportPosition(e);
/// CanvasPoint canvasPos = transformer.ToCanvas(viewportPos);
/// 
/// // Render something at the canvas position
/// ViewportPoint renderPos = transformer.ToViewport(canvasPos);
/// context.DrawEllipse(brush, pen, renderPos.ToAvalonia(), 5, 5);
/// </code>
/// </example>
public interface ITypedCoordinateTransformer
{
  /// <summary>
  /// The current zoom level (1.0 = 100%).
  /// </summary>
  double Zoom { get; }

  /// <summary>
  /// The X component of the viewport offset.
  /// </summary>
  double OffsetX { get; }

  /// <summary>
  /// The Y component of the viewport offset.
  /// </summary>
  double OffsetY { get; }

  #region Point Transforms

  /// <summary>
  /// Converts a viewport point to canvas coordinates.
  /// </summary>
  /// <param name="viewport">Point in viewport space.</param>
  /// <returns>Equivalent point in canvas space.</returns>
  CanvasPoint ToCanvas(ViewportPoint viewport);

  /// <summary>
  /// Converts a canvas point to viewport coordinates.
  /// </summary>
  /// <param name="canvas">Point in canvas space.</param>
  /// <returns>Equivalent point in viewport space.</returns>
  ViewportPoint ToViewport(CanvasPoint canvas);

  #endregion

  #region Vector/Delta Transforms

  /// <summary>
  /// Converts a viewport vector to canvas space.
  /// Only applies zoom, not offset (since vectors are relative).
  /// </summary>
  CanvasVector ToCanvas(ViewportVector viewport);

  /// <summary>
  /// Converts a canvas vector to viewport space.
  /// Only applies zoom, not offset (since vectors are relative).
  /// </summary>
  ViewportVector ToViewport(CanvasVector canvas);

  #endregion

  #region Rectangle Transforms

  /// <summary>
  /// Converts a viewport rectangle to canvas space.
  /// </summary>
  CanvasRect ToCanvas(ViewportRect viewport);

  /// <summary>
  /// Converts a canvas rectangle to viewport space.
  /// </summary>
  ViewportRect ToViewport(CanvasRect canvas);

  #endregion
}

/// <summary>
/// Default implementation of <see cref="ITypedCoordinateTransformer"/>.
/// </summary>
public class TypedCoordinateTransformer : ITypedCoordinateTransformer
{
  /// <inheritdoc />
  public double Zoom { get; private set; }

  /// <inheritdoc />
  public double OffsetX { get; private set; }

  /// <inheritdoc />
  public double OffsetY { get; private set; }

  /// <summary>
  /// Creates a new transformer with the specified viewport state.
  /// </summary>
  public TypedCoordinateTransformer(double zoom, double offsetX, double offsetY)
  {
    Zoom = zoom;
    OffsetX = offsetX;
    OffsetY = offsetY;
  }

  /// <summary>
  /// Updates the transformer with new viewport state.
  /// </summary>
  public void Update(double zoom, double offsetX, double offsetY)
  {
    Zoom = zoom;
    OffsetX = offsetX;
    OffsetY = offsetY;
  }

  #region Point Transforms

  /// <inheritdoc />
  public CanvasPoint ToCanvas(ViewportPoint viewport)
  {
    return new CanvasPoint(
        (viewport.X - OffsetX) / Zoom,
        (viewport.Y - OffsetY) / Zoom);
  }

  /// <inheritdoc />
  public ViewportPoint ToViewport(CanvasPoint canvas)
  {
    return new ViewportPoint(
        canvas.X * Zoom + OffsetX,
        canvas.Y * Zoom + OffsetY);
  }

  #endregion

  #region Vector/Delta Transforms

  /// <inheritdoc />
  public CanvasVector ToCanvas(ViewportVector viewport)
  {
    return new CanvasVector(viewport.DX / Zoom, viewport.DY / Zoom);
  }

  /// <inheritdoc />
  public ViewportVector ToViewport(CanvasVector canvas)
  {
    return new ViewportVector(canvas.DX * Zoom, canvas.DY * Zoom);
  }

  #endregion

  #region Rectangle Transforms

  /// <inheritdoc />
  public CanvasRect ToCanvas(ViewportRect viewport)
  {
    var topLeft = ToCanvas(viewport.TopLeft);
    var size = ToCanvas(new ViewportVector(viewport.Width, viewport.Height));
    return new CanvasRect(topLeft.X, topLeft.Y, size.DX, size.DY);
  }

  /// <inheritdoc />
  public ViewportRect ToViewport(CanvasRect canvas)
  {
    var topLeft = ToViewport(canvas.TopLeft);
    var size = ToViewport(new CanvasVector(canvas.Width, canvas.Height));
    return new ViewportRect(topLeft.X, topLeft.Y, size.DX, size.DY);
  }

  #endregion
}

/// <summary>
/// Extension methods for <see cref="ITypedCoordinateTransformer"/>.
/// </summary>
public static class TypedCoordinateTransformerExtensions
{
  /// <summary>
  /// Gets the visible canvas area for the current viewport.
  /// </summary>
  /// <param name="transformer">The coordinate transformer.</param>
  /// <param name="viewportSize">The size of the viewport.</param>
  /// <returns>The visible rectangle in canvas coordinates.</returns>
  public static CanvasRect GetVisibleCanvasRect(
      this ITypedCoordinateTransformer transformer,
      ViewportRect viewportSize)
  {
    return transformer.ToCanvas(viewportSize);
  }

  /// <summary>
  /// Checks if a canvas point is currently visible in the viewport.
  /// </summary>
  public static bool IsVisible(
      this ITypedCoordinateTransformer transformer,
      CanvasPoint canvasPoint,
      ViewportRect viewportBounds)
  {
    var viewportPoint = transformer.ToViewport(canvasPoint);
    return viewportBounds.Contains(viewportPoint);
  }

  /// <summary>
  /// Checks if a canvas rectangle is at least partially visible in the viewport.
  /// </summary>
  public static bool IsVisible(
      this ITypedCoordinateTransformer transformer,
      CanvasRect canvasRect,
      ViewportRect viewportBounds)
  {
    var viewportRect = transformer.ToViewport(canvasRect);
    return viewportBounds.Intersects(viewportRect);
  }
}
