namespace FlowGraph.Core.Elements.Shapes;

/// <summary>
/// An ellipse/circle shape element that can be rendered on the canvas.
/// </summary>
/// <remarks>
/// <para>
/// Ellipses are useful for:
/// - State diagram start/end states
/// - Circular annotations
/// - Icon backgrounds
/// - Decision points
/// </para>
/// <para>
/// The ellipse is defined by its bounding box (Position, Width, Height).
/// For a perfect circle, set Width == Height.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a start state circle
/// var startState = new EllipseElement("start")
/// {
///     Position = new Point(50, 100),
///     Width = 30,
///     Height = 30,
///     Fill = "#2ecc71",
///     Stroke = "#27ae60",
///     StrokeWidth = 2
/// };
/// 
/// // Create an oval annotation
/// var oval = new EllipseElement("highlight")
/// {
///     Position = new Point(100, 200),
///     Width = 150,
///     Height = 80,
///     Fill = null,  // No fill
///     Stroke = "#e74c3c",
///     StrokeWidth = 3
/// };
/// </code>
/// </example>
public class EllipseElement : ShapeElement
{
  /// <summary>
  /// Creates a new ellipse element with a generated ID.
  /// </summary>
  public EllipseElement() : this(Guid.NewGuid().ToString())
  {
  }

  /// <summary>
  /// Creates a new ellipse element with the specified ID.
  /// </summary>
  /// <param name="id">The unique identifier for this ellipse.</param>
  public EllipseElement(string id) : base(id)
  {
  }

  /// <inheritdoc />
  public override string Type => "ellipse";

  /// <summary>
  /// Gets the center X coordinate of the ellipse.
  /// </summary>
  public double CenterX => Position.X + (Width ?? 0) / 2;

  /// <summary>
  /// Gets the center Y coordinate of the ellipse.
  /// </summary>
  public double CenterY => Position.Y + (Height ?? 0) / 2;

  /// <summary>
  /// Gets the horizontal radius of the ellipse.
  /// </summary>
  public double RadiusX => (Width ?? 0) / 2;

  /// <summary>
  /// Gets the vertical radius of the ellipse.
  /// </summary>
  public double RadiusY => (Height ?? 0) / 2;

  /// <summary>
  /// Creates an ellipse centered at the specified point with the given radii.
  /// </summary>
  /// <param name="centerX">X coordinate of the center.</param>
  /// <param name="centerY">Y coordinate of the center.</param>
  /// <param name="radiusX">Horizontal radius.</param>
  /// <param name="radiusY">Vertical radius.</param>
  /// <returns>A new EllipseElement positioned at the center point.</returns>
  public static EllipseElement CreateCentered(double centerX, double centerY, double radiusX, double radiusY)
  {
    return new EllipseElement
    {
      Position = new Point(centerX - radiusX, centerY - radiusY),
      Width = radiusX * 2,
      Height = radiusY * 2
    };
  }

  /// <summary>
  /// Creates a circle centered at the specified point with the given radius.
  /// </summary>
  /// <param name="centerX">X coordinate of the center.</param>
  /// <param name="centerY">Y coordinate of the center.</param>
  /// <param name="radius">Radius of the circle.</param>
  /// <returns>A new EllipseElement configured as a circle.</returns>
  public static EllipseElement CreateCircle(double centerX, double centerY, double radius)
  {
    return CreateCentered(centerX, centerY, radius, radius);
  }
}
