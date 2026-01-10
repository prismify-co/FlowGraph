namespace FlowGraph.Core.Elements;

/// <summary>
/// Represents a rectangle with position and size.
/// Used for element bounds calculations.
/// </summary>
/// <param name="X">The X coordinate of the top-left corner.</param>
/// <param name="Y">The Y coordinate of the top-left corner.</param>
/// <param name="Width">The width of the rectangle.</param>
/// <param name="Height">The height of the rectangle.</param>
public readonly record struct Rect(double X, double Y, double Width, double Height)
{
  /// <summary>
  /// Gets the left edge X coordinate.
  /// </summary>
  public double Left => X;

  /// <summary>
  /// Gets the top edge Y coordinate.
  /// </summary>
  public double Top => Y;

  /// <summary>
  /// Gets the right edge X coordinate.
  /// </summary>
  public double Right => X + Width;

  /// <summary>
  /// Gets the bottom edge Y coordinate.
  /// </summary>
  public double Bottom => Y + Height;

  /// <summary>
  /// Gets the center point of the rectangle.
  /// </summary>
  public Point Center => new(X + Width / 2, Y + Height / 2);

  /// <summary>
  /// Creates an empty rectangle at the origin.
  /// </summary>
  public static Rect Empty => new(0, 0, 0, 0);

  /// <summary>
  /// Determines whether this rectangle contains the specified point.
  /// </summary>
  public bool Contains(Point point) =>
      point.X >= X && point.X <= Right &&
      point.Y >= Y && point.Y <= Bottom;

  /// <summary>
  /// Determines whether this rectangle intersects with another rectangle.
  /// </summary>
  public bool Intersects(Rect other) =>
      Left < other.Right && Right > other.Left &&
      Top < other.Bottom && Bottom > other.Top;

  /// <summary>
  /// Returns a new rectangle expanded by the specified amount on all sides.
  /// </summary>
  public Rect Inflate(double amount) =>
      new(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);

  /// <summary>
  /// Returns the union of this rectangle with another.
  /// </summary>
  public Rect Union(Rect other)
  {
    var left = Math.Min(Left, other.Left);
    var top = Math.Min(Top, other.Top);
    var right = Math.Max(Right, other.Right);
    var bottom = Math.Max(Bottom, other.Bottom);
    return new Rect(left, top, right - left, bottom - top);
  }
}
