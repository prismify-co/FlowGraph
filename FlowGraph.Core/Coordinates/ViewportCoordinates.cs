using System.Diagnostics;

namespace FlowGraph.Core.Coordinates;

/// <summary>
/// A point in viewport coordinate space (screen/control coordinates).
/// 
/// <para>
/// Viewport coordinates represent positions within the visible canvas control.
/// (0,0) is the top-left of the canvas control, and values increase to the right and down.
/// These coordinates change as the user pans and zooms - a node at canvas (100, 200) will
/// appear at different viewport positions depending on the current zoom/pan state.
/// </para>
/// 
/// <para>
/// <b>When to use:</b>
/// <list type="bullet">
/// <item>Auto-pan edge detection (checking if cursor is near viewport edges)</item>
/// <item>Fixed UI positioning (tooltips, menus that don't pan/zoom)</item>
/// <item>Direct rendering to DrawingContext (after converting from canvas)</item>
/// <item>Viewport bounds calculations</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Important:</b> Viewport coordinates assume (0,0) is at the canvas control's origin.
/// If the canvas is embedded in a larger control with margins/toolbars, you may need
/// to account for offsets when converting from parent control coordinates.
/// </para>
/// </summary>
[DebuggerDisplay("Viewport({X}, {Y})")]
public readonly record struct ViewportPoint(double X, double Y)
{
  /// <summary>
  /// The origin point (0, 0) in viewport space.
  /// </summary>
  public static ViewportPoint Zero => new(0, 0);

  /// <summary>
  /// Adds a vector to this point, producing a new point.
  /// </summary>
  public static ViewportPoint operator +(ViewportPoint point, ViewportVector vector)
      => new(point.X + vector.DX, point.Y + vector.DY);

  /// <summary>
  /// Subtracts a vector from this point, producing a new point.
  /// </summary>
  public static ViewportPoint operator -(ViewportPoint point, ViewportVector vector)
      => new(point.X - vector.DX, point.Y - vector.DY);

  /// <summary>
  /// Calculates the vector between two points.
  /// </summary>
  public static ViewportVector operator -(ViewportPoint a, ViewportPoint b)
      => new(a.X - b.X, a.Y - b.Y);

  /// <summary>
  /// Calculates the distance to another point.
  /// </summary>
  public double DistanceTo(ViewportPoint other)
  {
    var dx = X - other.X;
    var dy = Y - other.Y;
    return Math.Sqrt(dx * dx + dy * dy);
  }

  /// <summary>
  /// Calculates the squared distance to another point (faster than DistanceTo).
  /// </summary>
  public double DistanceSquaredTo(ViewportPoint other)
  {
    var dx = X - other.X;
    var dy = Y - other.Y;
    return dx * dx + dy * dy;
  }

  /// <summary>
  /// Checks if this point is within a distance from an edge of a bounding box.
  /// Useful for auto-pan edge detection.
  /// </summary>
  /// <param name="bounds">The viewport bounds to check against.</param>
  /// <param name="edgeDistance">The distance from edge to trigger.</param>
  /// <returns>True if the point is within edgeDistance of any edge.</returns>
  public bool IsNearEdge(ViewportRect bounds, double edgeDistance)
  {
    return X < edgeDistance ||
           X > bounds.Width - edgeDistance ||
           Y < edgeDistance ||
           Y > bounds.Height - edgeDistance;
  }

  /// <summary>
  /// Gets the direction(s) in which this point is near the edge.
  /// </summary>
  /// <param name="bounds">The viewport bounds to check against.</param>
  /// <param name="edgeDistance">The distance from edge to trigger.</param>
  /// <returns>A vector indicating the pan direction (negative = pan in that direction).</returns>
  public ViewportVector GetEdgePanDirection(ViewportRect bounds, double edgeDistance)
  {
    double dx = 0, dy = 0;

    if (X < edgeDistance) dx = 1;  // Near left, pan right
    else if (X > bounds.Width - edgeDistance) dx = -1;  // Near right, pan left

    if (Y < edgeDistance) dy = 1;  // Near top, pan down
    else if (Y > bounds.Height - edgeDistance) dy = -1;  // Near bottom, pan up

    return new ViewportVector(dx, dy);
  }

  /// <inheritdoc />
  public override string ToString() => $"Viewport({X:F1}, {Y:F1})";
}

/// <summary>
/// A vector/delta in viewport coordinate space.
/// 
/// <para>
/// Viewport vectors represent movements and distances in screen/control space.
/// They are affected by zoom - the same canvas distance appears as a larger
/// viewport distance at higher zoom levels.
/// </para>
/// </summary>
[DebuggerDisplay("ViewportΔ({DX}, {DY})")]
public readonly record struct ViewportVector(double DX, double DY)
{
  /// <summary>
  /// A zero-length vector.
  /// </summary>
  public static ViewportVector Zero => new(0, 0);

  /// <summary>
  /// The length (magnitude) of this vector.
  /// </summary>
  public double Length => Math.Sqrt(DX * DX + DY * DY);

  /// <summary>
  /// The squared length of this vector (faster than Length).
  /// </summary>
  public double LengthSquared => DX * DX + DY * DY;

  /// <summary>
  /// Returns a normalized (unit length) version of this vector.
  /// </summary>
  public ViewportVector Normalized()
  {
    var len = Length;
    return len > 0 ? new ViewportVector(DX / len, DY / len) : Zero;
  }

  /// <summary>
  /// Scales this vector by a scalar value.
  /// </summary>
  public static ViewportVector operator *(ViewportVector vector, double scalar)
      => new(vector.DX * scalar, vector.DY * scalar);

  /// <summary>
  /// Scales this vector by a scalar value.
  /// </summary>
  public static ViewportVector operator *(double scalar, ViewportVector vector)
      => new(vector.DX * scalar, vector.DY * scalar);

  /// <summary>
  /// Adds two vectors.
  /// </summary>
  public static ViewportVector operator +(ViewportVector a, ViewportVector b)
      => new(a.DX + b.DX, a.DY + b.DY);

  /// <summary>
  /// Subtracts two vectors.
  /// </summary>
  public static ViewportVector operator -(ViewportVector a, ViewportVector b)
      => new(a.DX - b.DX, a.DY - b.DY);

  /// <summary>
  /// Negates this vector.
  /// </summary>
  public static ViewportVector operator -(ViewportVector vector)
      => new(-vector.DX, -vector.DY);

  /// <inheritdoc />
  public override string ToString() => $"ViewportΔ({DX:F1}, {DY:F1})";
}

/// <summary>
/// A rectangle in viewport coordinate space.
/// 
/// <para>
/// Viewport rectangles represent bounded regions in screen/control space.
/// The most common use is representing the viewport bounds itself (the visible area).
/// </para>
/// </summary>
[DebuggerDisplay("Viewport[{X},{Y} {Width}x{Height}]")]
public readonly record struct ViewportRect(double X, double Y, double Width, double Height)
{
  /// <summary>
  /// An empty rectangle at the origin.
  /// </summary>
  public static ViewportRect Empty => new(0, 0, 0, 0);

  /// <summary>
  /// The top-left corner of the rectangle.
  /// </summary>
  public ViewportPoint TopLeft => new(X, Y);

  /// <summary>
  /// The top-right corner of the rectangle.
  /// </summary>
  public ViewportPoint TopRight => new(X + Width, Y);

  /// <summary>
  /// The bottom-left corner of the rectangle.
  /// </summary>
  public ViewportPoint BottomLeft => new(X, Y + Height);

  /// <summary>
  /// The bottom-right corner of the rectangle.
  /// </summary>
  public ViewportPoint BottomRight => new(X + Width, Y + Height);

  /// <summary>
  /// The center of the rectangle.
  /// </summary>
  public ViewportPoint Center => new(X + Width / 2, Y + Height / 2);

  /// <summary>
  /// The right edge X coordinate.
  /// </summary>
  public double Right => X + Width;

  /// <summary>
  /// The bottom edge Y coordinate.
  /// </summary>
  public double Bottom => Y + Height;

  /// <summary>
  /// Whether this rectangle is empty (has zero or negative area).
  /// </summary>
  public bool IsEmpty => Width <= 0 || Height <= 0;

  /// <summary>
  /// Checks if this rectangle contains a point.
  /// </summary>
  public bool Contains(ViewportPoint point) =>
      point.X >= X && point.X <= X + Width &&
      point.Y >= Y && point.Y <= Y + Height;

  /// <summary>
  /// Checks if this rectangle intersects with another rectangle.
  /// </summary>
  public bool Intersects(ViewportRect other) =>
      X < other.Right && Right > other.X &&
      Y < other.Bottom && Bottom > other.Y;

  /// <summary>
  /// Returns a rectangle inflated by the specified amount on all sides.
  /// </summary>
  public ViewportRect Inflate(double amount) =>
      new(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);

  /// <summary>
  /// Creates a viewport rect representing the full viewport area at origin.
  /// </summary>
  /// <param name="width">Viewport width.</param>
  /// <param name="height">Viewport height.</param>
  public static ViewportRect FromSize(double width, double height) => new(0, 0, width, height);

  /// <inheritdoc />
  public override string ToString() => $"Viewport[{X:F1},{Y:F1} {Width:F1}x{Height:F1}]";
}
