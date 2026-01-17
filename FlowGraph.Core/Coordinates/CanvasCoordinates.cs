using System.Diagnostics;

namespace FlowGraph.Core.Coordinates;

/// <summary>
/// A point in canvas coordinate space (logical graph coordinates).
/// 
/// <para>
/// Canvas coordinates represent the logical position of elements in the graph.
/// Node positions, port centers, edge endpoints all use canvas coordinates.
/// These values are stable regardless of zoom/pan - a node at (100, 200) stays
/// at (100, 200) even when the user zooms or pans.
/// </para>
/// 
/// <para>
/// <b>When to use:</b>
/// <list type="bullet">
/// <item>Node positions (node.Position)</item>
/// <item>Port centers calculated from node geometry</item>
/// <item>Edge start/end points</item>
/// <item>Hit testing against graph elements</item>
/// <item>Selection bounds in graph space</item>
/// </list>
/// </para>
/// </summary>
/// <example>
/// <code>
/// var nodePosition = CanvasPoint.FromNode(node);
/// var portCenter = renderModel.GetPortPosition(node, port, isOutput);
/// 
/// // Type-safe - compiler prevents mixing with ViewportPoint
/// CanvasPoint canvasPos = context.Coordinates.GetPointerCanvasPosition(e);
/// </code>
/// </example>
[DebuggerDisplay("Canvas({X}, {Y})")]
public readonly record struct CanvasPoint(double X, double Y)
{
  /// <summary>
  /// The origin point (0, 0) in canvas space.
  /// </summary>
  public static CanvasPoint Zero => new(0, 0);

  /// <summary>
  /// Creates a canvas point from a node's position.
  /// </summary>
  public static CanvasPoint FromNode(Node node) => new(node.Position.X, node.Position.Y);

  /// <summary>
  /// Creates a canvas point from the core Point type.
  /// </summary>
  public static CanvasPoint FromCore(Point point) => new(point.X, point.Y);

  /// <summary>
  /// Converts to the core Point type.
  /// </summary>
  public Point ToCore() => new(X, Y);

  /// <summary>
  /// Adds a vector to this point, producing a new point.
  /// </summary>
  public static CanvasPoint operator +(CanvasPoint point, CanvasVector vector)
      => new(point.X + vector.DX, point.Y + vector.DY);

  /// <summary>
  /// Subtracts a vector from this point, producing a new point.
  /// </summary>
  public static CanvasPoint operator -(CanvasPoint point, CanvasVector vector)
      => new(point.X - vector.DX, point.Y - vector.DY);

  /// <summary>
  /// Calculates the vector between two points.
  /// </summary>
  public static CanvasVector operator -(CanvasPoint a, CanvasPoint b)
      => new(a.X - b.X, a.Y - b.Y);

  /// <summary>
  /// Calculates the distance to another point.
  /// </summary>
  public double DistanceTo(CanvasPoint other)
  {
    var dx = X - other.X;
    var dy = Y - other.Y;
    return Math.Sqrt(dx * dx + dy * dy);
  }

  /// <summary>
  /// Calculates the squared distance to another point (faster than DistanceTo).
  /// </summary>
  public double DistanceSquaredTo(CanvasPoint other)
  {
    var dx = X - other.X;
    var dy = Y - other.Y;
    return dx * dx + dy * dy;
  }

  /// <summary>
  /// Returns a new point with both coordinates rounded to the nearest integer.
  /// </summary>
  public CanvasPoint Rounded() => new(Math.Round(X), Math.Round(Y));

  /// <summary>
  /// Returns a new point snapped to a grid.
  /// </summary>
  public CanvasPoint SnappedToGrid(double gridSize)
  {
    return new CanvasPoint(
        Math.Round(X / gridSize) * gridSize,
        Math.Round(Y / gridSize) * gridSize);
  }

  /// <summary>
  /// Linearly interpolates between two points.
  /// </summary>
  public static CanvasPoint Lerp(CanvasPoint a, CanvasPoint b, double t)
  {
    return new CanvasPoint(
        a.X + (b.X - a.X) * t,
        a.Y + (b.Y - a.Y) * t);
  }

  /// <inheritdoc />
  public override string ToString() => $"Canvas({X:F1}, {Y:F1})";
}

/// <summary>
/// A vector/delta in canvas coordinate space.
/// 
/// <para>
/// Canvas vectors represent movements, offsets, and distances in canvas space.
/// Unlike canvas points, vectors are relative (they represent the difference
/// between two points, not an absolute position).
/// </para>
/// 
/// <para>
/// <b>When to use:</b>
/// <list type="bullet">
/// <item>Drag distances in canvas space</item>
/// <item>Node movement offsets</item>
/// <item>Snap offsets</item>
/// <item>Collision avoidance adjustments</item>
/// </list>
/// </para>
/// </summary>
[DebuggerDisplay("CanvasΔ({DX}, {DY})")]
public readonly record struct CanvasVector(double DX, double DY)
{
  /// <summary>
  /// A zero-length vector.
  /// </summary>
  public static CanvasVector Zero => new(0, 0);

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
  /// Returns Zero if this vector has zero length.
  /// </summary>
  public CanvasVector Normalized()
  {
    var len = Length;
    return len > 0 ? new CanvasVector(DX / len, DY / len) : Zero;
  }

  /// <summary>
  /// Scales this vector by a scalar value.
  /// </summary>
  public static CanvasVector operator *(CanvasVector vector, double scalar)
      => new(vector.DX * scalar, vector.DY * scalar);

  /// <summary>
  /// Scales this vector by a scalar value.
  /// </summary>
  public static CanvasVector operator *(double scalar, CanvasVector vector)
      => new(vector.DX * scalar, vector.DY * scalar);

  /// <summary>
  /// Divides this vector by a scalar value.
  /// </summary>
  public static CanvasVector operator /(CanvasVector vector, double scalar)
      => new(vector.DX / scalar, vector.DY / scalar);

  /// <summary>
  /// Adds two vectors.
  /// </summary>
  public static CanvasVector operator +(CanvasVector a, CanvasVector b)
      => new(a.DX + b.DX, a.DY + b.DY);

  /// <summary>
  /// Subtracts two vectors.
  /// </summary>
  public static CanvasVector operator -(CanvasVector a, CanvasVector b)
      => new(a.DX - b.DX, a.DY - b.DY);

  /// <summary>
  /// Negates this vector.
  /// </summary>
  public static CanvasVector operator -(CanvasVector vector)
      => new(-vector.DX, -vector.DY);

  /// <summary>
  /// Creates a vector from a core Point (treating it as a delta).
  /// </summary>
  public static CanvasVector FromCore(Point delta) => new(delta.X, delta.Y);

  /// <summary>
  /// Converts to a core Point (for APIs that expect Point as delta).
  /// </summary>
  public Point ToCore() => new(DX, DY);

  /// <inheritdoc />
  public override string ToString() => $"CanvasΔ({DX:F1}, {DY:F1})";
}

/// <summary>
/// A rectangle in canvas coordinate space.
/// 
/// <para>
/// Canvas rectangles represent bounded regions in the graph coordinate space.
/// Node bounds, selection areas, and visible regions are expressed as canvas rects.
/// </para>
/// </summary>
[DebuggerDisplay("Canvas[{X},{Y} {Width}x{Height}]")]
public readonly record struct CanvasRect(double X, double Y, double Width, double Height)
{
  /// <summary>
  /// An empty rectangle at the origin.
  /// </summary>
  public static CanvasRect Empty => new(0, 0, 0, 0);

  /// <summary>
  /// The top-left corner of the rectangle.
  /// </summary>
  public CanvasPoint TopLeft => new(X, Y);

  /// <summary>
  /// The top-right corner of the rectangle.
  /// </summary>
  public CanvasPoint TopRight => new(X + Width, Y);

  /// <summary>
  /// The bottom-left corner of the rectangle.
  /// </summary>
  public CanvasPoint BottomLeft => new(X, Y + Height);

  /// <summary>
  /// The bottom-right corner of the rectangle.
  /// </summary>
  public CanvasPoint BottomRight => new(X + Width, Y + Height);

  /// <summary>
  /// The center of the rectangle.
  /// </summary>
  public CanvasPoint Center => new(X + Width / 2, Y + Height / 2);

  /// <summary>
  /// The right edge X coordinate.
  /// </summary>
  public double Right => X + Width;

  /// <summary>
  /// The bottom edge Y coordinate.
  /// </summary>
  public double Bottom => Y + Height;

  /// <summary>
  /// The size of the rectangle as a vector.
  /// </summary>
  public CanvasVector Size => new(Width, Height);

  /// <summary>
  /// Whether this rectangle is empty (has zero or negative area).
  /// </summary>
  public bool IsEmpty => Width <= 0 || Height <= 0;

  /// <summary>
  /// Checks if this rectangle contains a point.
  /// </summary>
  public bool Contains(CanvasPoint point) =>
      point.X >= X && point.X <= X + Width &&
      point.Y >= Y && point.Y <= Y + Height;

  /// <summary>
  /// Checks if this rectangle fully contains another rectangle.
  /// </summary>
  public bool Contains(CanvasRect other) =>
      other.X >= X && other.Right <= Right &&
      other.Y >= Y && other.Bottom <= Bottom;

  /// <summary>
  /// Checks if this rectangle intersects with another rectangle.
  /// </summary>
  public bool Intersects(CanvasRect other) =>
      X < other.Right && Right > other.X &&
      Y < other.Bottom && Bottom > other.Y;

  /// <summary>
  /// Returns the intersection of this rectangle with another.
  /// Returns Empty if they don't intersect.
  /// </summary>
  public CanvasRect Intersect(CanvasRect other)
  {
    var x1 = Math.Max(X, other.X);
    var y1 = Math.Max(Y, other.Y);
    var x2 = Math.Min(Right, other.Right);
    var y2 = Math.Min(Bottom, other.Bottom);

    if (x2 > x1 && y2 > y1)
      return new CanvasRect(x1, y1, x2 - x1, y2 - y1);

    return Empty;
  }

  /// <summary>
  /// Returns the smallest rectangle that contains both this and another rectangle.
  /// </summary>
  public CanvasRect Union(CanvasRect other)
  {
    if (IsEmpty) return other;
    if (other.IsEmpty) return this;

    var x1 = Math.Min(X, other.X);
    var y1 = Math.Min(Y, other.Y);
    var x2 = Math.Max(Right, other.Right);
    var y2 = Math.Max(Bottom, other.Bottom);

    return new CanvasRect(x1, y1, x2 - x1, y2 - y1);
  }

  /// <summary>
  /// Returns a rectangle inflated by the specified amount on all sides.
  /// </summary>
  public CanvasRect Inflate(double amount) =>
      new(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);

  /// <summary>
  /// Returns a rectangle inflated by different horizontal and vertical amounts.
  /// </summary>
  public CanvasRect Inflate(double horizontal, double vertical) =>
      new(X - horizontal, Y - vertical, Width + horizontal * 2, Height + vertical * 2);

  /// <summary>
  /// Returns a rectangle offset by a vector.
  /// </summary>
  public CanvasRect Offset(CanvasVector vector) =>
      new(X + vector.DX, Y + vector.DY, Width, Height);

  /// <summary>
  /// Creates a rectangle from two corner points.
  /// </summary>
  public static CanvasRect FromPoints(CanvasPoint a, CanvasPoint b)
  {
    var minX = Math.Min(a.X, b.X);
    var minY = Math.Min(a.Y, b.Y);
    var maxX = Math.Max(a.X, b.X);
    var maxY = Math.Max(a.Y, b.Y);
    return new CanvasRect(minX, minY, maxX - minX, maxY - minY);
  }

  /// <summary>
  /// Creates a rectangle from a center point and size.
  /// </summary>
  public static CanvasRect FromCenter(CanvasPoint center, double width, double height) =>
      new(center.X - width / 2, center.Y - height / 2, width, height);

  /// <summary>
  /// Creates a rectangle from a core Rect.
  /// </summary>
  public static CanvasRect FromCore(Elements.Rect rect) =>
      new(rect.X, rect.Y, rect.Width, rect.Height);

  /// <summary>
  /// Converts to a core Rect.
  /// </summary>
  public Elements.Rect ToCore() => new(X, Y, Width, Height);

  /// <inheritdoc />
  public override string ToString() => $"Canvas[{X:F1},{Y:F1} {Width:F1}x{Height:F1}]";
}
